// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BuildTools {
    /// <summary>
    /// Simplified regex creation. Escapes dots, expands "*", and conversts forward slashes to escaped backslashes. Compiled, case-insensitive, culture-invariant.
    /// </summary>
    public class Pattern:Regex {
        /// <summary>
        /// Si
        /// </summary>
        /// <param name="pattern"></param>
        public Pattern(string pattern):base(pattern.Replace("/","\\\\").Replace(".","\\.").Replace("*",".*"), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline) {

        }


    }
}
