// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace COMInstaller {
    public class DllCollection:Dictionary<string,Dll>{
        /// <summary>
        /// Builds a dictionary of DLL names to DLL references. Searches the specified directories in order - elements are not overwritten.
        /// </summary>
        /// <param name="dirs"></param>
        public DllCollection(string[] dirs):base(StringComparer.OrdinalIgnoreCase) {
            foreach (string s in dirs) {
                if (!Directory.Exists(s)) continue;
                string[] files = Directory.GetFiles(s, "*.dll");
                foreach (string f in files) {
                    string key = Path.GetFileNameWithoutExtension(f);
                    if (!this.ContainsKey(key)) this[key] =  new Dll(f);
                }
            }
        }


    }
}
