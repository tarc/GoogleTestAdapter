﻿using System;
using System.Collections.Generic;
using GoogleTestAdapter.Settings;

namespace GoogleTestAdapter.Helpers
{
    public class RegexTraitParser
    {
        private readonly TestEnvironment _testEnvironment;


        public RegexTraitParser(TestEnvironment testEnvironment)
        {
            _testEnvironment = testEnvironment;
        }


        public List<RegexTraitPair> ParseTraitsRegexesString(string option)
        {
            var result = new List<RegexTraitPair>();

            string[] pairs = option.Split(
                new[] { SettingsWrapper.TraitsRegexesPairSeparator },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                try
                {
                    result.Add(ParseRegexTraitPair(pair));
                }
                catch (Exception e)
                {
                    _testEnvironment.Logger.LogError(
                        "Could not parse pair '" + pair + "', exception message: " + e.Message);
                }
            }

            return result;
        }


        private RegexTraitPair ParseRegexTraitPair(string pair)
        {
            string[] values = pair.Split(
                new[] { SettingsWrapper.TraitsRegexesRegexSeparator }, StringSplitOptions.None);
            string[] trait = values[1].Split(
                new[] { SettingsWrapper.TraitsRegexesTraitSeparator }, StringSplitOptions.None);
            string regex = values[0];
            string traitName = trait[0];
            string traitValue = trait[1];
            return new RegexTraitPair(regex, traitName, traitValue);
        }

    }

}