/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections;

namespace ImageResizer.Collections {
  public class ReverseEnumerable<T>:IEnumerable<T> {
        private ReadOnlyCollection<T> _collection;
        public ReverseEnumerable(ReadOnlyCollection<T> collection){
            _collection = collection;
        }
        public IEnumerator<T> GetEnumerator() {
            return new ReverseEnumerator<T>(_collection);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return new ReverseEnumerator<T>(_collection);
        }
    }
    public class ReverseEnumerator<T> : IEnumerator<T> {
        private ReadOnlyCollection<T> _collection;
        private int curIndex;
        private T curItem;


        public ReverseEnumerator(ReadOnlyCollection<T> collection) {
            _collection = collection;
            curIndex = _collection.Count;
            curItem = default(T);

        }

        public bool MoveNext() {
            curIndex--;
            //Avoids going beyond the beginning of the collection.
            if (curIndex < 0) {
                curItem = default(T);
                return false;
            } else {
                // Set current box to next item in collection.
                curItem = _collection[curIndex];
                return true;
            }
        }

        public void Reset() { curIndex = _collection.Count; curItem = default(T); }

        void IDisposable.Dispose() { }

        public T Current {
            get { return curItem; }
        }


        object IEnumerator.Current {
            get { return Current; }
        }

    }
}
