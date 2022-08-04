// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
using System.Collections.ObjectModel;


namespace ImageResizer.Collections
{
    public class ReverseEnumerable<T> : Imazen.Common.Collections.ReverseEnumerable<T>
    {
        public ReverseEnumerable(ReadOnlyCollection<T> collection) : base(collection)
        {
        }
    }

    public class ReverseEnumerator<T> : Imazen.Common.Collections.ReverseEnumerator<T>
    {

        public ReverseEnumerator(ReadOnlyCollection<T> collection) : base(collection)
        {
        }
    }
}
