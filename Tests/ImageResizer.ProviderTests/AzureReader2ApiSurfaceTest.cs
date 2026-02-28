// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using ImageResizer.Plugins.AzureReader2;
using PublicApiGenerator;
using Xunit;

namespace ImageResizer.ProviderTests {
    public class AzureReader2ApiSurfaceTest {
        [Fact]
        public void PublicApi_MatchesApprovedSurface() {
            var assembly = typeof(AzureReader2Plugin).Assembly;
            var publicApi = assembly.GeneratePublicApi();

            // Strip assembly-level attributes (BuildDate, Commit, Edition, Guid, TargetFramework)
            // as these change between builds
            var normalized = Regex.Replace(publicApi, @"^\[assembly:.*\]\s*\r?\n", "", RegexOptions.Multiline);

            // Read the approved API from embedded resource
            var resourceName = "ImageResizer.ProviderTests.AzureReader2.approved.txt";
            string approved;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
                Assert.NotNull(stream);
                using (var reader = new StreamReader(stream)) {
                    approved = reader.ReadToEnd();
                }
            }

            // Normalize line endings for cross-platform comparison
            approved = approved.Replace("\r\n", "\n");
            normalized = normalized.Replace("\r\n", "\n");

            Assert.Equal(approved.Trim(), normalized.Trim());
        }
    }
}
