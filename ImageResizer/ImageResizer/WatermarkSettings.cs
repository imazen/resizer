/**
 * Written by Nathanael Jones 
 * http://nathanaeljones.com
 * nathanael.jones@gmail.com
 * 
 * This file is for user extension and modification (although all the source is!)
 * No restrictions on distribution of this file.
 * 
 **/
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Drawing;

namespace fbs.ImageResizer
{
    /// <summary>
    /// Provides extensibility points for drawing watermarks and even modifying resizing/image settings
    /// </summary>
    public class WatermarkSettings
    {
        /// <summary>
        /// Creates a new WatermarkSettings class
        /// </summary>
        /// <param name="q"></param>
        public WatermarkSettings(NameValueCollection q)
        {
           
        }

        /// <summary>
        /// Executed prior to resizing. Permits modifications of geometry and effect settings
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="opts"></param>
        public virtual void ModifySettings(ResizeSettings rs, ImageSettings opts, ImageFilter adjustments, ImageOutputSettings ios )
        {
        }

        /// <summary>
        /// Modify this to do watermarks. Executing after resizing is complete
        /// </summary>
        /// <param name="b"></param>
        /// <param name="g"></param>
        public virtual void Process(Bitmap b, Graphics g){
            TrialWatermark(b,g);
        }

        private void TrialWatermark(Bitmap b, Graphics g)
        {
            //Only executes when built using "Trial Version" configuration
#if TRIAL
            if (new Random().Next(4) < 2)
                this.DrawString(b, g, "Unlicensed", FontFamily.GenericSansSerif, Color.FromArgb(70, Color.White));
#endif
        }

        public virtual void DrawString(Bitmap b, Graphics g, String text, FontFamily ff, Color c)
        {
            SizeF size = g.MeasureString(text, new Font(ff, 32));
            double difX = (size.Width - b.Width) / -size.Width;

            double difY = (size.Height - b.Height) / -size.Height;
            float finalFontSize = 32 + (float)(32 * Math.Min(difX, difY));
            SizeF finalSize = g.MeasureString(text, new Font(ff, finalFontSize));

            g.DrawString(text, new Font(ff, finalFontSize), new SolidBrush(c),
                new PointF((b.Width - finalSize.Width) / 2, (b.Height - finalSize.Height) / 2));
            g.Flush();
        }
    }
}
