using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ImageResizer.Plugins.SeamCarving {

    /// <summary>
    /// Plugin to perform Cair Job Seam Carving
    /// </summary>
    public class CairJob {

        /// <summary>
        /// Initialize new instance of Cair Job class
        /// </summary>
        public CairJob() {
            Threads = 1;
            Timeout = 5000; //5 seconds
        }

        /// <summary>
        /// Get or set the source path
        /// </summary>
        public string SourcePath{get;set;}

        /// <summary>
        /// Get or set the weight path
        /// </summary>
        public string WeightPath { get; set; }

        /// <summary>
        /// Get or set the destination path
        /// </summary>
        public string DestPath{get;set;}

        /// <summary>
        /// Get or set the desired size
        /// </summary>
         public Size Size{get;set;}

        /// <summary>
        /// Gets or sets the Seam Carving Filter type
        /// </summary>
         public SeamCarvingPlugin.FilterType Filter{get;set;}

        /// <summary>
        /// Gets or sets the Seam Carving energy
        /// </summary>
        public SeamCarvingPlugin.EnergyType Energy{get;set;}
        /// <summary>
        /// Whether to carve or display the energy functions
        /// </summary>
        public SeamCarvingPlugin.OutputType Output{get;set;}
        /// <summary>
        /// How many ms to wait before killing the processes
        /// </summary>
        public int Timeout{get;set;}
        /// <summary>
        /// How many threads to use
        /// </summary>
        public int Threads { get; set; }
    }
}
