using ImageResizer.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.FFmpeg
{
    /// <summary>
    /// Dynamically extract frames from videos by time or percentage. Includes basic blank frame avoidance. Based on ffmpeg.
    /// </summary>
    public class FFmpegJob
    {
        /// <summary>
        /// Creates a new instance of FFmpegJob, and forces a timeout after 15 seconds.
        /// </summary>
        public FFmpegJob()
        {
            Timeout = 15000; //15 seconds
        }


        public FFmpegJob(NameValueCollection query) :this()
        {

            Seconds = ParseUtils.ParsePrimitive<double>(query["ffmpeg.seconds"]);
            Percent = ParseUtils.ParsePrimitive<double>(query["ffmpeg.percent"]);

            SkipBlankFrames = ParseUtils.ParsePrimitive<bool>(query["ffmpeg.skipblankframes"]);

        }


        public string SourcePath { get; set; }

        public Stream Result { get; set; }

        /// <summary>
        /// If true, frames will be energy-anlayzed to verify they're not blank. 
        /// </summary>
        public bool? SkipBlankFrames { get; set; }

        /// <summary>
        /// How many seconds to FF when a blank frame is found
        /// </summary>
        public double? IncrementWhenBlank { get; set; }

        /// <summary>
        /// How many seconds within the video to grab the frame
        /// </summary>
        public double? Seconds { get; set; }

        /// <summary>
        /// What percentage within the video to grab a frame. Using this instead of seconds will result in slower execution, as the video size will have to be retrieved first.
        /// </summary>
        public double? Percent { get; set; }

        /// <summary>
        /// How many milliseconds to wait before timing out
        /// </summary>
        public int Timeout { get; set; }
    }
}
