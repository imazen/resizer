// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Text;
using System.Web.Hosting;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ComplexWebApplication
{
    public partial class WhitespaceTrimmerTest : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var dir = Path.Combine(
                Path.Combine(Path.GetDirectoryName(HostingEnvironment.ApplicationPhysicalPath.TrimEnd('/', '\\')),
                    "Images"), "private");
            if (!Directory.Exists(dir)) return;
            var files = Directory.GetFiles(dir, "*.jpg");
            var sb = new StringBuilder();
            foreach (var s in files)
                sb.Append("<img src='/private/" + Path.GetFileName(s) +
                          "?width=200' /><img src='/private/" + Path.GetFileName(s) +
                          "?width=200&trim.threshold=80&trim.percentpadding=1'/><br/>");
            lit.Text = sb.ToString();
            lit.Mode = LiteralMode.PassThrough;
        }
    }
}