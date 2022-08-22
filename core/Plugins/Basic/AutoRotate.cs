// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using ImageResizer.Configuration;
using ImageResizer.Resizing;

namespace ImageResizer.Plugins.Basic
{
    [Obsolete("This plugin does nothing; autorotate is now built-in.")]
    public class AutoRotate : BuilderExtension, IPlugin
    {
        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }
    }
}