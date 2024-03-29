// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Drawing;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using ImageResizer.Resizing;

namespace MyCode.MyPlugins
{
    /// <summary>
    ///     Writes the text specified in the 'sample' querystring value
    /// </summary>
    public class SamplePlugin : BuilderExtension, IPlugin, IQuerystringPlugin
    {
        protected override RequestedAction OnProcess(ImageState s)
        {
            return base.OnProcess(s);
            // If we wanted to modify the querystring or settings before the pipleine started, 
            // we could do it here (like the ModifySettings method of WatermarkSettings.cs in V2)
        }

        protected override RequestedAction RenderOverlays(ImageState s)
        {
            // Get 'sample' from the querystring
            var sample = s.settings["sample"];

            //Don't try to draw the string if it is empty, or if this is a 'simulation' render
            if (string.IsNullOrEmpty(sample) || s.destGraphics == null) return RequestedAction.None;

            //Let's use Arial, and make sure the text fits inside the bitmap width.
            var font = new Font("Arial",
                (float)(s.destBitmap.Width / (sample.Length * 1.5f)), GraphicsUnit.Pixel);

            //Draw the text at the top left corner of the resulting image.
            s.destGraphics.DrawString(sample, font, Brushes.Black, 0, 0);

            return RequestedAction.None;
        }

        public SamplePlugin()
        {
        }

        private Config c;

        public IPlugin Install(Config c)
        {
            //Allows '<add name="MyCode.MyPlugins.SamplePlugin" />' to work
            c.Plugins.add_plugin(this);
            this.c = c;
            return this;
        }

        public bool Uninstall(Config c)
        {
            //Allows '<remove name="MyCode.MyPlugins.SamplePlugin" />' to work
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys()
        {
            return
                new string[]
                    { "sample" }; //So the pipeline knows to handle image requests that use this querystring key
        }
    }
}