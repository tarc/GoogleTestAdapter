﻿using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

namespace NewProjectWizard.GTA.Helpers
{
    public static class VisualStudioHelper
    {
        public static string GetPlatformToolsetFromVisualStudioVersion()
        {
            DTE dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));
            switch (dte.Version)
            {
                case "10.0": return "v100";
                case "11.0": return "v110";
                case "12.0": return "v120";
                case "14.0": return "v140";
                case "15.0": return "v141";
                default: throw new InvalidOperationException($"'{dte.Version}' is not a valid version for GTA");
            }
        }

        public static string GetGenerateDebugInformationFromVisualStudioVersion()
        {
            DTE dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));
            return dte.Version == "15.0" ? "DebugFull" : "true";
        }

        public static string GetVariadicMaxFromVisualStudioVersion()
        {
            DTE dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE));
            switch (dte.Version)
            {
                case "10.0":
                case "11.0":
                case "12.0":
                    return "_VARIADIC_MAX=10;";
                default:
                    return "";
            }
        }


    }
}