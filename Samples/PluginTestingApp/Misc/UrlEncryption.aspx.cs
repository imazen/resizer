// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ImageResizer.Plugins.Encrypted;

namespace ComplexWebApplication.Misc {
    public partial class UrlEncryption : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            img.ImageUrl = EncryptedPlugin.First.EncryptPathAndQuery("~/fountain-small.jpg?width=100");
        }
    }
}