/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Configuration.Xml {
    /// <summary>
    /// Encapsulates a node/attribute selection query, such as "node.node.attribute"
    /// </summary>
    public class Selector:List<string> {
        public Selector(List<string> items) {
            this.AddRange(items);
        }
        public Selector(string selector):base(selector.Trim('.').Split(new char[]{'.'}, StringSplitOptions.RemoveEmptyEntries)) {
            
        }

        public string Last {
            get{
                if (this.Count > 0) return this[this.Count - 1];
                return null;
            }
        }
        /// <summary>
        /// Returns a subset of the list starting at the specified index
        /// </summary>
        /// <param name="startAt"></param>
        /// <returns></returns>
        public Selector GetSublist(int startAt) {
            return new Selector(this.GetRange(startAt, this.Count - startAt));
        }

        public Selector GetRemainder() {
            if (this.Count < 2) return null;
            return GetSublist(1);
        }

        public Selector GetAllExceptLast() {
            if (this.Count < 2) return null;
            return new Selector(this.GetRange(0, this.Count - 1));
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Count; i++) {
                sb.Append(this[i]);
                if (i < Count - 1) sb.Append('.');
            }
            return sb.ToString();
        }
    }
}
