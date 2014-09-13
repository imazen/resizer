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
    /// Captures frames from a MPEG video
    /// </summary>
    public class FFmpegJob
    {

        /// <summary>
        /// Creates an instance of the ffmpegJob class and sets the default timeout to 15 seconds
        /// </summary>
        public FFmpegJob()
        {
            Timeout = 15000; //15 seconds
        }

        /// <summary>
        ///Sets up capture values for Seconds and Percent from the given query
        ///The keys and values that can be used to query for capturing frames from the MPEG
        /// </summary>
        /// <param name="query">capture keys and values query</param>
        public FFmpegJob(NameValueCollection query) :this()
        {

            Seconds = ParseUtils.ParsePrimitive<double>(query["ffmpeg.seconds"]);
            Percent = ParseUtils.ParsePrimitive<double>(query["ffmpeg.percent"]);

            SkipBlankFrames = ParseUtils.ParsePrimitive<bool>(query["ffmpeg.skipblankframes"]);

        }

        /// <summary>
        /// Path to the MPEG data
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// Memory Stream of the MPEG video
        /// </summary>
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
