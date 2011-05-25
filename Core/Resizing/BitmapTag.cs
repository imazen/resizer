using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

namespace ImageResizer.Resizing {
    public class BitmapTag {

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
