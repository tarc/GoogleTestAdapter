﻿using GoogleTestAdapter.Common;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.Helpers
{

    public class TestEnvironment
    {

        public SettingsWrapper Options { get; }
        public ILogger Logger { get; }


        public TestEnvironment(SettingsWrapper options, ILogger logger) 
        {
            Options = options;
            Logger = logger;
        }


    }

}