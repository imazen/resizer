// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Drawing;
using System.Web;
using ImageResizer.Util;
using Xunit;

namespace ImageResizer.Core.Tests
{
    public class ResizeSettingsTest
    {
        [Fact]
        public void TestCropValues()
        {
            var s = new ResizeSettings();
            s.CropBottomRight = new PointF(50, 50);
            s.CropTopLeft = new PointF(0, 0);
            Assert.Equal<string>("?crop=0,0,50,50", s.ToString());
            s.CropMode = CropMode.Auto;
            Assert.Equal<string>("?crop=auto", s.ToString());
        }


        [Fact]
        public void TestIntParsingOfInvalidSyntax()
        {
            var s = new ResizeSettings("maxwidth=100px&maxheight=50.6");
            Assert.Equal<int>(-1, s.MaxWidth);
            Assert.Equal<int>(-1, s.MaxHeight);
        }

        [Theory]
        [InlineData("red", "red")]
        [InlineData("#44550099", "44550099")]
        [InlineData("666", "666666")]
        [InlineData("6663", "66666630")]
        [InlineData("green", "Green")]
        [InlineData("fefefe", "fefefe")]
        [InlineData("#fefefecc", "fefefecc")]
        [InlineData("gggaeee", "transparent")]
        public void TestBgColor(string from, string to)
        {
            var s = new ResizeSettings("bgcolor=" + HttpUtility.UrlEncode(from));
            s.BackgroundColor = s.BackgroundColor;
            Assert.Equal(to, s["bgcolor"], StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("red", "red")]
        [InlineData("#44550099", "44550099")]
        [InlineData("#fefefecc", "fefefecc")]
        public void TestRoundTripColor(string from, string expected)
        {
            Color? parsed = ParseUtils.ParseColor(from).Value;
            Assert.Equal(expected, ParseUtils.SerializeColor(parsed.Value), StringComparer.InvariantCultureIgnoreCase);
        }

        [Theory]
        [InlineData("a=1", "", "?a=1")]
        [InlineData("", "b=2", "?b=2")]
        [InlineData("a=1", "b=2", "?a=1&b=2")]
        [InlineData("b=1", "b=2", "?b=1")]
        public void TestMergingConstructor(string q, string defaults, string expected)
        {
            var defaultSettings = new ResizeSettings(defaults);
            var mergedSettings = new ResizeSettings(q, defaultSettings);
            var expectedSettings = new ResizeSettings(expected);

            Assert.Equal(expectedSettings.Count, mergedSettings.Count);
            foreach (var key in expectedSettings.AllKeys) Assert.Equal(expectedSettings[key], mergedSettings[key]);
        }
    }
}