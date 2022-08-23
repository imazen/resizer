// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using ImageResizer.Configuration;
using ImageResizer.Plugins.Basic;
using ImageResizer.Plugins.Imageflow;
using ImageResizer.Plugins.RemoteReader;
using ImageResizer.Util;
using Xunit;

namespace ImageResizer.AllPlugins.Tests
{
    public class TestAll
    {
        public static Config GetConfig()
        {
            var c = new Config();
            //c.Pipeline.s
            /*  WatermarkPlugin w = new ImageResizer.Plugins.Watermark.WatermarkPlugin();
              w.align = System.Drawing.ContentAlignment.BottomLeft;
              w.hideIfTooSmall = false;
              w.keepAspectRatio = true;
              w.valuesPercentages = true;
              w.watermarkDir = "~/"; //Where the watermark plugin looks for the image specified in the querystring ?watermark=file.png
              w.bottomRightPadding = new System.Drawing.SizeF(0, 0);
              w.topLeftPadding = new System.Drawing.SizeF(0, 0);
              w.watermarkSize = new System.Drawing.SizeF(1, 1); //The desired size of the watermark, maximum dimensions (aspect ratio maintained if keepAspectRatio = true)
              //Install the plugin
              w.Install(c);*/

            new Gradient().Install(c);
            var rrp = new RemoteReaderPlugin();
            rrp.Install(c);
            rrp.AllowRemoteRequest += delegate(object sender, RemoteRequestEventArgs args)
            {
                args.DenyRequest = false;
            }; //Doesn't support non-ASP.NET usage yet.


            new ImageflowBackendPlugin().Install(c);
            new DropShadow().Install(c);
            new VirtualFolder("/images", "..\\..\\..\\Samples\\Images", false).Install(c);


            //s3reader
            //sqlreader

            return c;
        }

        private static Dictionary<string, string[]> GetData()
        {
            var data = new Dictionary<string, string[]>();
            data.Add("width", new string[] { "-100", ".,,.,,", "40", "100", "800", "2" });
            data.Add("height", new string[] { "-100", ".,,.,,", "40", "100", "800", "2" });
            data.Add("maxwidth", new string[] { "-100", ".,,.,,", "100", "3300", "2" });
            data.Add("maxheight", new string[] { "-100", ".,,.,,", "100", "3300", "2" });
            data.Add("crop",
                new string[]
                    { "auto", "none", "(0,0,0,0)", "100,100,-100,100", "1000,1000,-1000,-1000", "10,10,50,50" });
            data.Add("stretch", new string[] { "fill", "proportionally", "huh" });
            data.Add("format", new string[] { "jpg", "png", "gif" });
            data.Add("quality", new string[] { "1", "80", "100" });
            data.Add("dither", new string[] { "true", "4pass", "none" });
            data.Add("colors", new string[] { "2", "128", "256" });
            data.Add("shadowWidth", new string[] { "0", "2", "100" });
            data.Add("shadowColor", new string[] { "black", "green", "gray" });
            data.Add("shadowOffset", new string[] { "-3,-1", "-10,-10", "0,0", "30,30" });
            data.Add("trim.threshold", new string[] { "0", "255", "80" });
            data.Add("trim.percentpadding", new string[] { "0", "1", "51", "100" });
            data.Add("carve", new string[] { "false", "true" });
            data.Add("filter",
                new string[]
                {
                    "grayscale", "sepia", "alpha(0)", "alpha(.5)", "alpha(abcef)", "brightness(-1)", "brightness(1)",
                    "brightness(-.01)"
                });
            data.Add("scale", new string[] { "downscaleonly", "upscaleonly", "upscalecanvas", "both" });
            data.Add("ignoreicc", new string[] { "true", "false" });
            data.Add("angle", new string[] { "0", "361", "15", "180" });
            data.Add("rotate", new string[] { "0", "90", "270", "400", "15", "45" });
            var colors = new string[]
            {
                "", "black", "white", ParseUtils.SerializeColor(Color.FromArgb(25, Color.Green)),
                ParseUtils.SerializeColor(Color.Transparent)
            };
            data.Add("color1", colors);
            data.Add("color2", colors);
            data.Add("page", new string[] { "-10", "1", "5" });
            data.Add("frame", new string[] { "-10", "1", "5" });
            data.Add("margin", new string[] { "-10", "1", "5", "100" });
            data.Add("borderWidth", new string[] { "-10", "1", "5", "100" });
            data.Add("paddingWidth", new string[] { "-10", "1", "5", "100" });
            data.Add("paddingColor", colors);
            data.Add("borderColor", colors);
            data.Add("bgcolor", colors);
            data.Add("flip", new string[] { "h", "v", "hv", "both", "none" });
            data.Add("sourceflip", new string[] { "h", "v", "hv", "both", "none" });
            data.Add("blur", new string[] { "0", "1", "5" });
            data.Add("sharpen", new string[] { "0", "1", "5" });
            data.Add("builder", new string[] { "default", "imageflow" });
            //TODO: add watermark, advanced filter, S3reader, sqlreader, remotereader
            //gradient.png: "color1","color2", "angle", "width", "height" 
            return data;
        }

        public static List<object> GetSourceObjects()
        {
            var sources = new List<object>();
            sources.Add("~/images/red-leaf.jpg");
            // sources.Add("/gradient.png");
            return sources;
        }

        public static IEnumerable<object[]> RandomCombinations
        {
            get
            {
                var data = GetData();
                var sources = GetSourceObjects();
                var r = new Random();
                var startedAt = DateTime.Now;
                for (var i = 0; i < 100; i++)
                {
                    var settings = new ResizeSettings();
                    foreach (var key in data.Keys)
                        if (r.Next(10) > 5)
                            settings[key] = data[key][r.Next(data[key].Length)];
                    //if (DateTime.Now.Subtract(startedAt).TotalMilliseconds > 2000) break;
                    yield return new object[] { sources[r.Next(sources.Count)], settings.ToString() };
                }
            }
        }

        private static Dictionary<string, Bitmap> cachedImages = new Dictionary<string, Bitmap>();


        private int counter = 0;

        [Theory(Skip = "Skip on CI")]
        [MemberData(nameof(RandomCombinations))]
        public void RandomTest(object source, string query)
        {
            var c = GetConfig();

            if (!Directory.Exists("test-images")) Directory.CreateDirectory("test-images");

            var fname = (counter + "_" + query).Replace('?', ';').Replace('&', ';');

            var dir = Path.GetFullPath("test-images\\");
            if (fname.Length + dir.Length > 250) fname = fname.Substring(0, 250 - dir.Length) + "...";
            var instructions = new Instructions(query);
            instructions["scache"] = "mem";
            c.CurrentImageBuilder.Build(new ImageJob(source as string, dir + fname, instructions, false, true));
            counter++;
        }

        private Random r = new Random();

        [Theory]
        [MemberData(nameof(RandomCombinations))]
        public void TestCombinationsFast(object source, string query)
        {
            var c = GetConfig();
            c.CurrentImageBuilder.GetFinalSize(new Size(r.Next(10000), r.Next(10000)), new ResizeSettings(query));
        }
    }
}