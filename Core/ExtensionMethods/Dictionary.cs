// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.ExtensionMethods
{
    internal static class DictionaryExtensions
    {
        internal static TK Get<TK>(this IDictionary<string, object> d, string key, TK defaultValue)
        {
            return d.ContainsKey(key) ? (TK)d[key] : defaultValue;
        }
    }
}
