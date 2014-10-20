using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

namespace ImageResizer.Resizing {
    /// <summary>
    /// 
    /// </summary>
    public class BitmapTag {

        public BitmapTag(object tag) {
            if (tag is string) _path = (string)tag;
            if (tag is BitmapTag) {
                _path = ((BitmapTag)tag).Path;
                _source = ((BitmapTag)tag).Source;
            }
        }

        public BitmapTag(string path, Stream source) {
            this._path = path;
            this._source = source;
        }
        private string _path = null;

        public string Path {
            get { return _path; }
            set { _path = value; }
        }
        private Stream _source = null;

        public Stream Source {
            get { return _source; }
            set { _source = value; }
        }
       
    }
}
