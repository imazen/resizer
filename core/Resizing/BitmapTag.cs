// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.IO;

namespace ImageResizer.Resizing
{
    /// <summary>
    /// Contains Path and Stream information so both can be stored in Bitmap.Tag.
    /// If a stream is garbage collected before the Bitmap that uses it, stuff will crash.
    /// </summary>
    public class BitmapTag
    {
        public BitmapTag(object tag)
        {
            if (tag is string) _path = (string)tag;
            if (tag is BitmapTag)
            {
                _path = ((BitmapTag)tag).Path;
                _source = ((BitmapTag)tag).Source;
            }
        }

        public BitmapTag(string path, Stream source)
        {
            _path = path;
            _source = source;
        }

        private string _path = null;

        public string Path
        {
            get => _path;
            set => _path = value;
        }

        private Stream _source = null;

        public Stream Source
        {
            get => _source;
            set => _source = value;
        }
    }
}