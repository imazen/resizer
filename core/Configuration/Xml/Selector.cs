// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Configuration.Xml
{
    /// <summary>
    ///     Encapsulates a node/attribute selection query, such as "node.node.attribute"
    /// </summary>
    public class Selector : List<string>
    {
        public Selector(List<string> items)
        {
            AddRange(items);
        }

        public Selector(string selector) : base(selector.Trim('.')
            .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries))
        {
        }

        public string Last
        {
            get
            {
                if (Count > 0) return this[Count - 1];
                return null;
            }
        }

        /// <summary>
        ///     Returns a subset of the list starting at the specified index
        /// </summary>
        /// <param name="startAt"></param>
        /// <returns></returns>
        public Selector GetSublist(int startAt)
        {
            return new Selector(GetRange(startAt, Count - startAt));
        }

        public Selector GetRemainder()
        {
            if (Count < 2) return null;
            return GetSublist(1);
        }

        public Selector GetAllExceptLast()
        {
            if (Count < 2) return null;
            return new Selector(GetRange(0, Count - 1));
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < Count; i++)
            {
                sb.Append(this[i]);
                if (i < Count - 1) sb.Append('.');
            }

            return sb.ToString();
        }
    }
}