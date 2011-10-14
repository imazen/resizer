using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Drawing;
using ImageResizer.Util;
using System.Text.RegularExpressions;

namespace ImageResizer.Plugins.Watermark {
    public class TextLayer:Layer {

        public TextLayer(){}
        public TextLayer(NameValueCollection attrs)
            : base(attrs) {
                Text = attrs["text"];
                Vertical = "true".Equals(attrs["vertical"], StringComparison.OrdinalIgnoreCase);
                TextColor = Utils.parseColor(attrs["color"], Color.Black);
        }

        private string _text = "";
        /// <summary>
        /// The text to display
        /// </summary>
        public string Text {
            get { return _text; }
            set { _text = value; }
        }

        private bool _vertical = false;
        /// <summary>
        /// If true, text will be displayed verticallt
        /// </summary>
        public bool Vertical {
            get { return _vertical; }
            set { _vertical = value; }
        }


        private Color _textColor = Color.Black;
        /// <summary>
        /// The color to draw the text
        /// </summary>
        public Color TextColor {
            get { return _textColor; }
            set { _textColor = value; }
        }

        public Font GetFont() {
            return new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold);
        }
        public StringFormat GetFormat() {
            System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat();
            if (Vertical) drawFormat.FormatFlags = StringFormatFlags.DirectionVertical;
            return drawFormat;
        }
        public Brush GetBrush() {
            return new SolidBrush(TextColor);
        }

        public override void RenderTo(Resizing.ImageState s) {
            if (string.IsNullOrEmpty(Text)) return;

            string finalText = Text;

            if (finalText.IndexOf('#') > -1) {
                Regex r = new Regex("\\#\\{([^}]+)\\}");
                finalText = r.Replace(finalText, delegate(Match m){
                    string val =  s.settings[m.Groups[1].Value];
                    if (val == null) return "";
                    else return val;
                });
            }

             RectangleF bounds = this.CalculateLayerCoordinates(s, delegate(double maxwidth, double maxheight) {
                 using (Font f= GetFont()){
                     return PolygonMath.RoundPoints(s.destGraphics.MeasureString(finalText, f, new PointF(), GetFormat()));
                 }
             }, true);
            using(Font f = GetFont()){
                using (Brush b = GetBrush()) {
                    s.destGraphics.DrawString(finalText, f, b, bounds.Location, GetFormat());
                }
            }
        }
    }
}
