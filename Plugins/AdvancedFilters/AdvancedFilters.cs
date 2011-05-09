/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer;
using ImageResizer.Resizing;
using AForge.Imaging.Filters;
namespace ImageResizer.Plugins.AdvancedFilters {
    public class AdvancedFilters:BuilderExtension, IPlugin {
        public AdvancedFilters() {
        }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }
        protected override RequestedAction PostRenderImage(ImageState s) {
            string blurRad = s.settings["blur"];
            int blurRadius = 0;
            if (!string.IsNullOrEmpty(blurRad) && int.TryParse(blurRad, out blurRadius)) {
                GaussianBlur b = new GaussianBlur(1.4, blurRadius);
                b.ApplyInPlace(s.destBitmap);
            }

            string sharpRad = s.settings["sharpen"];
            int sharpRadius = 0;
            if (!string.IsNullOrEmpty(sharpRad) && int.TryParse(sharpRad, out sharpRadius)) {
                GaussianSharpen gs = new GaussianSharpen(1.4, sharpRadius);
                gs.ApplyInPlace(s.destBitmap);
            }
            
            return RequestedAction.None;
        }
    }
}
