using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Drawing;
using ImageResizer.Util;
using System.Text.RegularExpressions;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.Watermark {
    public class TextLayer:Layer {

        public TextLayer(){}
        public TextLayer(NameValueCollection attrs)
            : base(attrs) {
                Text = attrs["text"];
                Vertical = attrs.Get("vertical",false);
                TextColor = ParseUtils.ParseColor(attrs["color"], TextColor);
                OutlineColor = ParseUtils.ParseColor(attrs["outlineColor"], OutlineColor);
                GlowColor = ParseUtils.ParseColor(attrs["glowColor"], GlowColor);
                Font = attrs["font"];
                Angle = attrs.Get("angle", Angle);
                FontSize = attrs.Get("fontSize", FontSize);
                Style = attrs.Get("style", this.Style);
                OutlineWidth = attrs.Get("outlineWidth", OutlineWidth);
                GlowWidth = attrs.Get("glowWidth", GlowWidth);
                Rendering = attrs.Get("rendering", this.Rendering);
        }


        public override object[] GetHashBasis()
        {
            var b = new object[] {Text, Vertical, TextColor,OutlineColor,GlowColor,Font,Angle,FontSize,Style,OutlineWidth,GlowWidth,Rendering };
            var o = base.GetHashBasis();
            object[] combined = new object[b.Length + o.Length];
            Array.Copy(b, combined, b.Length);
            Array.Copy(o, 0, combined, b.Length, o.Length);
            return combined;
        }

        private TextRenderingHint _rendering = TextRenderingHint.AntiAliasGridFit;

        public TextRenderingHint Rendering {
            get { return _rendering; }
            set { _rendering = value; }
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
        /// If true, text will be displayed vertically
        /// </summary>
        public bool Vertical {
            get { return _vertical; }
            set { _vertical = value; }
        }


        private double _angle = 0;
        /// <summary>
        /// Angle of clockwise rotation
        /// </summary>
        public double Angle {
            get { return _angle; }
            set { _angle = value; }
        }

        private Color _textColor = Color.Black;
        /// <summary>
        /// The color to draw the text
        /// </summary>
        public Color TextColor {
            get { return _textColor; }
            set { _textColor = value; }
        }

        private int _outlineWidth = 0;
        /// <summary>
        /// The width of the text ouline (OutlineColor)
        /// </summary>
        public int OutlineWidth {
            get { return _outlineWidth; }
            set { _outlineWidth = value; }
        }
        private int _glowWidth = 0;
        /// <summary>
        /// The width of the glow effect (GlowColor)
        /// </summary>
        public int GlowWidth {
            get { return _glowWidth; }
            set { _glowWidth = value; }
        }

        private Color _outlineColor = Color.White;

        /// <summary>
        /// The color of the outline
        /// </summary>
        public Color OutlineColor {
            get { return _outlineColor; }
            set { _outlineColor = value; }
        }
        private Color _glowColor;
        /// <summary>
        /// The color of the glow effect. 
        /// </summary>
        public Color GlowColor {
            get { return _glowColor; }
            set { _glowColor = value; }
        }

        private string _font = null;

        /// <summary>
        /// The name of the font
        /// </summary>
        public string Font {
            get { return _font; }
            set { _font = value; }
        }

        private int _fontSize = 48;
        /// <summary>
        /// The size of the font in pixels
        /// </summary>  
        public int FontSize {
            get { return _fontSize; }
            set { _fontSize = value; }
        }


        private FontStyle _style = FontStyle.Bold;
        /// <summary>
        /// The font style
        /// </summary>
        public FontStyle Style {
            get { return _style; }
            set { _style = value; }
        }

        /// <summary>
        /// Sets the font for the text layer. Default is Generic Sans Serif.
        /// </summary>
        /// <returns></returns>
        public Font GetFont() {
            FontFamily ff = string.IsNullOrEmpty(Font) ? FontFamily.GenericSansSerif : new FontFamily(Font);
            
            return new Font(ff, FontSize, Style, GraphicsUnit.Pixel);

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

            SizeF naturalSize = SizeF.Empty;
            SizeF unrotatedSize = SizeF.Empty;
             RectangleF bounds = this.CalculateLayerCoordinates(s, delegate(double maxwidth, double maxheight) {
                 using (Font f= GetFont())
                 using (StringFormat sf = GetFormat()){
                    naturalSize = s.destGraphics.MeasureString(finalText, f, new PointF(), sf);
                     SizeF size = naturalSize;

                     unrotatedSize = Fill ? PolygonMath.ScaleInside(size, new SizeF((float)maxwidth, (float)maxheight)) : size;

                     if (Angle != 0) size = PolygonMath.GetBoundingBox(PolygonMath.RotatePoly(PolygonMath.ToPoly(new RectangleF(new PointF(0, 0), size)), Angle)).Size;
                     if (Fill) {
                         size = PolygonMath.ScaleInside(size, new SizeF((float)maxwidth, (float)maxheight));
                     }
                     f.FontFamily.Dispose();
                     return PolygonMath.RoundPoints(size);
                     
                 }
             }, true);
             using (Font f = GetFont()) {

                 s.destGraphics.SmoothingMode = SmoothingMode.HighQuality;
                 s.destGraphics.TextRenderingHint = Rendering; // Utils.parseEnum<TextRenderingHint>(s.settings["watermark.rendering"], this.Rendering); ;
                 s.destGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                 s.destGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                 s.destGraphics.CompositingMode = CompositingMode.SourceOver;
                 s.destGraphics.CompositingQuality = CompositingQuality.HighQuality;

                 s.destGraphics.ResetTransform();
                 if (Angle != 0) s.destGraphics.RotateTransform((float)Angle);
                 s.destGraphics.ScaleTransform(unrotatedSize.Width / naturalSize.Width, unrotatedSize.Height / naturalSize.Height);
                 s.destGraphics.TranslateTransform(bounds.X, bounds.Y, MatrixOrder.Append);
                 using (StringFormat sf = GetFormat()) {
                     DrawString(s.destGraphics, finalText, f, new Point(0, 0), sf);
                 }
                 s.destGraphics.ResetTransform();

                 f.FontFamily.Dispose();
             }
        }

        public void DrawString(Graphics g, string text, Font f, Point point, StringFormat fmt) {
            if (GlowWidth == 0 && OutlineWidth == 0) {
                using (Brush b = GetBrush()) {
                    g.DrawString(text, f, b, new PointF(0, 0), fmt);
                }
                return;
            }

            using (GraphicsPath path = new GraphicsPath()) {
                path.AddString(text, f.FontFamily, (int)f.Style, (float)(f.SizeInPoints / 72 * g.DpiY), point, fmt);

                Color c = GlowColor;
                if (GlowWidth > 0 && c.A == 255) c = Color.FromArgb(Math.Max(64, Math.Min(24, 255 / GlowWidth)), c);
                //Draw glow
                for (int i = 1; i <= GlowWidth; ++i) {
                    using (Pen pen = new Pen(c, i + OutlineWidth)) {
                        pen.LineJoin = LineJoin.Round;
                        g.DrawPath(pen, path);
                    }
                }
                //Draw outline
                if (OutlineWidth > 0) {
                    using (Pen pen = new Pen(OutlineColor, OutlineWidth)) {
                        pen.LineJoin = LineJoin.Round;
                        g.DrawPath(pen, path);
                    }
                }
                //Draw inner text
                using (Brush b = GetBrush()) {
                    g.FillPath(b, path);
                }

            }

        }
    }
}
