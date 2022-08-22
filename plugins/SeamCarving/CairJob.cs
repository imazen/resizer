// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ImageResizer.Plugins.SeamCarving {
    public class CairJob {
        public CairJob() {
            Threads = 1;
            Timeout = 5000; //5 seconds
        }

        public string SourcePath{get;set;}
        public string WeightPath { get; set; }
        public string DestPath{get;set;}

         public Size Size{get;set;}
         public SeamCarvingPlugin.FilterType Filter{get;set;}

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
