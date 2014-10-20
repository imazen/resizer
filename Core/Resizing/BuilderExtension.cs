/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Resizing {
    /// <summary>
    /// Provides a useable base class that can be used to modify the behavior of ImageBuilder.
    /// When registered with an ImageBuilder instance, the ImageBuilder will call the corresponding methods on the extension prior to executing its own methods. 
    /// </summary>
    public class BuilderExtension : AbstractImageProcessor{
    }
}
