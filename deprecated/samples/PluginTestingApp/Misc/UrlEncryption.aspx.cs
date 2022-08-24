// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Web.UI;
using ImageResizer.Plugins.Encrypted;

namespace ComplexWebApplication.Misc
{
    public partial class UrlEncryption : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            img.ImageUrl = EncryptedPlugin.First.EncryptPathAndQuery("~/fountain-small.jpg?width=100");
        }
    }
}