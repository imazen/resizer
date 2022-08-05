// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using ImageResizer.Configuration.Issues;
using Xunit;

namespace ImageResizer.Configuration.Xml
{
    public class XmlTests
    {
        [Theory]
        [InlineData("<resizEr><DiskCACHE aTTr='valUE' /></resizEr>", "diskCache.attr",
            "valUE")] //Verify case-insensitivity
        public void TestCachedQueryAttr(string xml, string selector, string expectedValue)
        {
            var s = new IssueSink("XmlTests");
            var n = Node.FromXmlFragment(xml, s); //Node, all start and end tags must match in case. XML rules.
            Assert.Equal(expectedValue, n.queryAttr(selector));
            var issues = s.GetIssues();
            if (issues != null)
                foreach (var issue in issues)
                    Debug.Write(issue.ToString());
        }
    }
}