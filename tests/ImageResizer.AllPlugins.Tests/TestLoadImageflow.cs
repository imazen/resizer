// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using Xunit;
using Xunit.Abstractions;

namespace ImageResizer.AllPlugins.Tests {
 
    public class TestLoadingImageflow {
        private readonly ITestOutputHelper _testOutputHelper;

        public TestLoadingImageflow(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void TestImageflow() {


            using (var j = new Imageflow.Bindings.JobContext())
            {
                var ver = j.GetVersionInfo();
                _testOutputHelper.WriteLine("Loaded Imageflow " + ver.LongVersionString);
            }
        }
    }
}