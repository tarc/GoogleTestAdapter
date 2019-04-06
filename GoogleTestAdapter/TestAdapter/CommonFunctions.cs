﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Helpers;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Framework;
using GoogleTestAdapter.TestAdapter.Settings;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace GoogleTestAdapter.TestAdapter
{
    public static class CommonFunctions
    {
        public const string GtaSettingsEnvVariable = "GTA_FALLBACK_SETTINGS";

        public static void ReportErrors(ILogger logger, string phase, OutputMode outputMode)
        {
            IList<string> errors = logger.GetMessages(Severity.Error, Severity.Warning);
            if (errors.Count == 0)
                return;

            bool hasErrors = logger.GetMessages(Severity.Error).Count > 0;
            string hint = outputMode > OutputMode.Info
                ? ""
                : " (enable debug mode for more information)";
            string jointErrors = string.Join(Environment.NewLine, errors);

            string message = $"{Environment.NewLine}================{Environment.NewLine}" 
                + $"The following errors and warnings occured during {phase}{hint}:{Environment.NewLine}" 
                + jointErrors;

            if (hasErrors)
                logger.LogError(message);
            else
                logger.LogWarning(message);
        }

        public static void CreateEnvironment(IRunSettings runSettings, IMessageLogger messageLogger, out ILogger logger, out SettingsWrapper settings, string solutionDir = null)
        {
            if (string.IsNullOrWhiteSpace(solutionDir))
            {
                solutionDir = null;
            }

            var settingsProvider = SafeGetRunSettingsProvider(runSettings, messageLogger);

            var ourRunSettings = GetRunSettingsContainer(settingsProvider, messageLogger);
            foreach (RunSettings projectSettings in ourRunSettings.ProjectSettings)
            {
                projectSettings.GetUnsetValuesFrom(ourRunSettings.SolutionSettings);
            }

            var settingsWrapper = new SettingsWrapper(ourRunSettings, solutionDir);

            var loggerAdapter = new VsTestFrameworkLogger(messageLogger, () => settingsWrapper.OutputMode, () => settingsWrapper.TimestampOutput);
            var regexParser = new RegexTraitParser(loggerAdapter);
            settingsWrapper.RegexTraitParser = regexParser;

            LogWarningsForDeprecatedSettings(ourRunSettings, loggerAdapter);

            settings = settingsWrapper;
            logger = loggerAdapter;
        }

        private static RunSettingsProvider SafeGetRunSettingsProvider(IRunSettings runSettings, IMessageLogger messageLogger)
        {
            try
            {
                return runSettings.GetSettings(GoogleTestConstants.SettingsName) as RunSettingsProvider;
            }
            catch (Exception e)
            {
                string errorMessage =
                    $"ERROR: Visual Studio test framework failed to provide settings. Error message: {e.Message}";
                // if fallback settings are configured, we do not want to make the tests fail
                var level = AreFallbackSettingsConfigured() ? TestMessageLevel.Informational : TestMessageLevel.Error;

                messageLogger.SendMessage(level, errorMessage);
                return null;
            }
        }

        private static RunSettingsContainer GetRunSettingsContainer(RunSettingsProvider settingsProvider,
            IMessageLogger messageLogger)
        {
            RunSettingsContainer ourRunSettings;
            if (settingsProvider != null)
            {
                ourRunSettings = settingsProvider.SettingsContainer;
            }
            else
            {
                ourRunSettings = GetRunSettingsFromEnvVariable(messageLogger);
                if (ourRunSettings == null)
                {
                    messageLogger.SendMessage(TestMessageLevel.Warning, "Warning: Using default settings.");
                    ourRunSettings = new RunSettingsContainer();
                }
            }

            return ourRunSettings;
        }

        private static RunSettingsContainer GetRunSettingsFromEnvVariable(IMessageLogger messageLogger)
        {
            string settingsFile;
            try
            {
                settingsFile = Environment.GetEnvironmentVariable(GtaSettingsEnvVariable);
                if (settingsFile == null)
                {
                    messageLogger.SendMessage(TestMessageLevel.Informational, $"No settings file provided through env variable {GtaSettingsEnvVariable}");
                    return null;
                }
            }
            catch (Exception e)
            {
                messageLogger.SendMessage(TestMessageLevel.Error, $"ERROR: Exception while trying to acces env variable {GtaSettingsEnvVariable}, message: {e.Message}");
                return null;
            }
                
            try
            {
                if (!File.Exists(settingsFile))
                {
                    messageLogger.SendMessage(TestMessageLevel.Warning,
                        $"Warning: Settings file is provided through env variable {GtaSettingsEnvVariable}, but file '{settingsFile}' does not exist");
                    return null;
                }

                var settingsContainer = new RunSettingsContainer();
                if (!settingsContainer.GetUnsetValuesFrom(settingsFile))
                {
                    messageLogger.SendMessage(TestMessageLevel.Warning,
                        $"Warning: Settings file is provided through env variable {GtaSettingsEnvVariable}, but file '{settingsFile}' could not be loaded");
                    return null;
                }

                messageLogger.SendMessage(TestMessageLevel.Informational,
                    $"Using fallback settings from file '{settingsFile}' (provided through env variable {GtaSettingsEnvVariable})");
                return settingsContainer;
            }
            catch (Exception e)
            {
                messageLogger.SendMessage(TestMessageLevel.Error, $"ERROR: Settings file is provided through env variable {GtaSettingsEnvVariable}, but an exception occured while trying to read file '{settingsFile}'. Exception message: {e.Message}");
                return null;
            }
        }

        private static void LogWarningsForDeprecatedSettings(RunSettingsContainer runSettingsContainer, ILogger logger)
        {
            var debugModeProperty = typeof(RunSettings).GetProperty(nameof(RunSettings.DebugMode));
            if (HasSetting(runSettingsContainer, debugModeProperty))
            {
                logger.LogWarning($"GTA option '{nameof(IGoogleTestAdapterSettings.DebugMode)}' does not have any effect any more - check your settings files and replace any occurence with new option '{nameof(IGoogleTestAdapterSettings.OutputMode)}' as follows:");
                logger.LogWarning($"<DebugMode>False</DebugMode> => <OutputMode>{OutputMode.Info}</OutputMode>");
                logger.LogWarning($"<DebugMode>True</DebugMode> => <OutputMode>{OutputMode.Verbose}</OutputMode> (consider using {OutputMode.Debug})");
            }

            var useNewTestExecutionEnvironmentProperty = typeof(RunSettings).GetProperty(nameof(RunSettings.UseNewTestExecutionFramework));
            if (HasSetting(runSettingsContainer, useNewTestExecutionEnvironmentProperty))
            {
                logger.LogWarning($"GTA option '{nameof(IGoogleTestAdapterSettings.UseNewTestExecutionFramework)}' does not have any effect any more - check your settings files and replace any occurence with new option '{nameof(IGoogleTestAdapterSettings.DebuggerKind)}' as follows:");
                logger.LogWarning($"<UseNewTestExecutionFramework>False</UseNewTestExecutionFramework> => <DebuggerKind>{DebuggerKind.VsTestFramework}</DebuggerKind>");
                logger.LogWarning($"<UseNewTestExecutionFramework>True</UseNewTestExecutionFramework> => <DebuggerKind>{DebuggerKind.Native}</DebuggerKind>");
            }

            var showReleaseNotesProperty = typeof(RunSettings).GetProperty(nameof(RunSettings.ShowReleaseNotes));
            if (HasSetting(runSettingsContainer, showReleaseNotesProperty))
            {
                logger.LogWarning($"GTA option '{nameof(IGoogleTestAdapterSettings.ShowReleaseNotes)}' is deprecated - check your settings files and remove any occurence.");
            }
        }

        private static bool HasSetting(RunSettingsContainer runSettingsContainer, PropertyInfo propertyInfo)
        {
            return propertyInfo.GetValue(runSettingsContainer.SolutionSettings) != null || 
                   runSettingsContainer.ProjectSettings.Any(s => propertyInfo.GetValue(s) != null);
        }

        public static void LogVisualStudioVersion(ILogger logger)
        {
            VsVersion version = VsVersionUtils.GetVisualStudioVersion(logger);
            switch (version)
            {
                // warning printed while checking version
                case VsVersion.Unknown:
                case VsVersion.VS2012:
                    return;
                default:
                    logger.DebugInfo($"Visual Studio Version: {version}");
                    break;
            }
        }

        private static bool AreFallbackSettingsConfigured()
        {
            try
            {
                return Environment.GetEnvironmentVariable(GtaSettingsEnvVariable) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}