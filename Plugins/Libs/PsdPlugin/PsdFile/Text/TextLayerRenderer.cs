/* Copyright (c) 2014 Imazen See license.txt for your rights */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace PhotoshopFile.Text
{
    public class TextLayerRenderer
    {
        Layer l = null;
        public TextLayerRenderer(Layer layer)
        {
            l = layer;
        }

        private bool _ignoreMissingFonts = true;

        public bool IgnoreMissingFonts {
            get { return _ignoreMissingFonts; }
            set { _ignoreMissingFonts = value; }
        }

        public void Render(Graphics g, Nullable<Color> overlayColor, string replacementText)
        {

            string text = null;
            bool horizontal = true;
            string fontName;
            double fontSize = 0;
            bool fauxBold = false;
            bool fauxItalic = false;
            bool underline = false;
            bool strikethrough = false;

            Color fillColor;
            bool fillFlag = false;
            Color strokeColor;
            bool strokeFlag = false;
            double outlineWidth = 0;

            bool fillFirst = true;

            Color glowColor = Color.Yellow;
            bool glowFlag = false;
            double glowWidth = 0;
            double glowOpacity = 1;
            
            Rectangle rect = l.Rect;
            StringAlignment hAlign = StringAlignment.Center;
            StringAlignment vAlign = StringAlignment.Center;


            //Parse information from file.
            Glow outerGlow = null;
            TypeToolObject tto = null;
            TypeTool tt = null;
            bool foundTxt2 = false;
            //Loop through adjustment info
            List<PhotoshopFile.Layer.AdjustmentLayerInfo> adjustments = l.AdjustmentInfo;
            for (int j = 0; j < adjustments.Count; j++)
            {
                if (adjustments[j].Key.Equals("TySh"))
                    tto = new TypeToolObject(adjustments[j]);
                if (adjustments[j].Key.Equals("oglw"))
                    outerGlow = new Glow(adjustments[j]);
                //if (adjustments[j].Key.Equals("iglw"))
                //    innerGlow = new Glow(adjustments[j]);
                if (adjustments[j].Key.Equals("lrFX"))
                    //throw new Exception("We don't parse these effects do we?");
                if (adjustments[j].Key.Equals("tySh"))
                    tt = new TypeTool(adjustments[j]);
                if (adjustments[j].Key.Equals("Txt2"))
                    foundTxt2 = true;
            }
            //We know the TySh format best. 
            if (tto != null){
                Dictionary<string,object> d = tto.StylesheetReader.GetStylesheetDataFromLongestRun();
                Matrix2D mat = tto.Transform;
                text = tto.StylesheetReader.Text;
                horizontal = tto.isTextHorizontal;
                fontName = TdTaParser.getString(tto.StylesheetReader.getFontSet()[(int)TdTaParser.query(d,"Font")],"Name$");
                fontSize = (double)TdTaParser.query(d,"FontSize");
                fauxBold = TdTaParser.getBool(d,"FauxBold");
                fauxItalic = TdTaParser.getBool(d,"FauxItalic");
                underline = TdTaParser.getBool(d,"Underline");
                strikethrough = TdTaParser.getBool(d,"Strikethrough");
                int styleRunAlignment = (int)TdTaParser.query(d,"StyleRunAlignment");//No idea what this maps to.
                //string info = tto.TxtDescriptor.getString();
                outlineWidth =  (double)TdTaParser.query(d,"OutlineWidth");

                fillColor = TdTaParser.getColor(d,"FillColor");
                strokeColor = TdTaParser.getColor(d,"StrokeColor");
                fillFlag = TdTaParser.getBool(d,"FillFlag");
                strokeFlag = TdTaParser.getBool(d,"StrokeFlag");
                fillFirst = TdTaParser.getBool(d,"FillFirst");


                //TODO: get alignment data
            }else if (tt != null){
                throw new Exception("Old style tySh font syntax not implemented, found on layer " + l.Name + ". Use a newer version of Photoshop");
            }else if (foundTxt2){
                throw new Exception("Txt2 text layer info not supported, found on layer " + l.Name + ". Where did you find this file? What version of photoshop?");
            }else{
                throw new Exception("No text layer information found on " + l.Name + "!");
            }

            if (outerGlow != null){
                glowColor = outerGlow.Color;
                glowFlag = outerGlow.Enabled;
                glowWidth = (int)outerGlow.Blur;
                glowOpacity = (double)outerGlow.Opacity / 255.0;
            }

            //Replace text if requested.
            if (replacementText != null) text = replacementText;

         

            //Fix newlines
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
          
            //Do graphics stuff
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            using(GraphicsPath path = new GraphicsPath()){
                FontFamily fontFamily = null;
                StringFormat strformat = null;
                try{
                    FontStyle style = FontStyle.Regular;
                    //Remove MT
                    if (fontName.EndsWith("MT")) fontName = fontName.Substring(0, fontName.Length - 2);
                    //Remove -Bold, -Italic, -BoldItalic
                    if (fontName.EndsWith("-Bold", StringComparison.OrdinalIgnoreCase)) style |= FontStyle.Bold;
                    if (fontName.EndsWith("-Italic", StringComparison.OrdinalIgnoreCase)) style |= FontStyle.Italic;
                    if (fontName.EndsWith("-BoldItalic", StringComparison.OrdinalIgnoreCase)) style |= FontStyle.Bold | FontStyle.Italic;
                    //Remove from fontName
                    fontName = new Regex("\\-(Bold|Italic|BoldItalic)$", RegexOptions.IgnoreCase | RegexOptions.IgnoreCase).Replace(fontName, "");
                    //Remove PS
                    if (fontName.EndsWith("PS")) fontName = fontName.Substring(0, fontName.Length - 2);
                    //Convert camel case fontName to spaced font name
                    fontName = Regex.Replace(fontName, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ");

                    //Find font family
                    try {
                        fontFamily = new FontFamily(fontName);
                    } catch (ArgumentException ae) {
                        if (IgnoreMissingFonts) {
                            fontFamily = FontFamily.GenericSansSerif;
                        } else throw ae;

                    }
                    if (fauxBold) style |= FontStyle.Bold;
                    if (fauxItalic) style |= FontStyle.Italic;
                    if (underline) style |= FontStyle.Underline;
                    if (strikethrough) style |= FontStyle.Strikeout;

                    strformat = new StringFormat();
                    strformat.LineAlignment = hAlign;
                    strformat.Alignment = vAlign;
                    strformat.FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip;
                    if (!horizontal) strformat.FormatFlags |= StringFormatFlags.DirectionVertical;
                    strformat.Trimming = StringTrimming.None;
                    
                    path.AddString(text, fontFamily,
                        (int)style, (float)(fontSize),rect, strformat);
                }finally{
                    fontFamily.Dispose();
                    strformat.Dispose();
                }

                if (glowFlag)
                {
                    if (glowWidth == 3) glowWidth = 2; //To replicate photoshop rounding bug
                  
                    double finalOpacity = (glowOpacity + 1.5)/2.5; //Because Photoshop doesn't actually use the opacity as the final opacity... 
                    //Add a 30% fade inside the glow width
                    int fadeWidth = (int)Math.Floor(glowWidth * 0.33);

                    double totalOpacity = 0; //Start at transparent

                    //start with outermost ring, work inwards
                    for (int i = fadeWidth; i >=0; i--)
                    {
                        //How much opacity do we lack to complete the desired opacity?
                        //totalOpacity = prevOpacity * (1-curOpacity) + curOpacity
                        //a=b *(1-c) + c
                        //>>> where b!=1, c=(b-a/b-1)
                        double missingOpacity = (totalOpacity - finalOpacity) / (totalOpacity - 1);

                        int remainingRings = fadeWidth;
                       
                        double currentOpacity = missingOpacity / (fadeWidth + 1);
                        //Update total opacity
                        totalOpacity = totalOpacity * (1 - currentOpacity) + currentOpacity;
                        //Draw it
                        using (Pen pen = new Pen(Color.FromArgb((int)Math.Round(currentOpacity * 255.0), glowColor), (float)((glowWidth - fadeWidth + (double)i) * 2)))
                        {
                            pen.LineJoin = LineJoin.Round;
                            g.DrawPath(pen, path);
                        }
                    }
                }

                if (fillFirst){
                    //Draw fill
                    if (fillFlag){
                        using(SolidBrush brush = new SolidBrush(fillColor)){
                            g.FillPath(brush, path);
                        }
                    }
                    //Draw stroke
                    if (strokeFlag){
                        using (Pen p = new Pen(strokeColor,(float)outlineWidth)){
                            p.LineJoin = LineJoin.Round;
                            g.DrawPath(p,path);
                        }
                    }
                }else{
                    //Draw stroke
                    if (strokeFlag){
                        using (Pen p = new Pen(strokeColor, (float)outlineWidth))
                        {
                            p.LineJoin = LineJoin.Round;
                            g.DrawPath(p,path);
                        }
                    }
                                        //Draw fill
                    if (fillFlag){
                        using(SolidBrush brush = new SolidBrush(fillColor)){
                            g.FillPath(brush, path);
                        }
                    }
                }
            }
 
        }
    }
}
