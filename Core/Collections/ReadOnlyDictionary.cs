using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Collections {
    public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue> {
        private IDictionary<TKey, TValue> _dictionary;

        public ReadOnlyDictionary() {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary) {
            _dictionary = dictionary;
        }

        #region IDictionary<TKey,TValue> Members

        public void Add(TKey key, TValue value) {
            throw new NotSupportedException("This dictionary is read-only");
        }

        public bool ContainsKey(TKey key) {
            return _dictionary.ContainsKey(key);
        }

        public ICollection<TKey> Keys {
            get { return _dictionary.Keys; }
        }

        public bool Remove(TKey key) {
            throw new NotSupportedException("This dictionary is read-only");
        }

        public bool TryGetValue(TKey key, out TValue value) {
            return _dictionary.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values {
            get { return _dictionary.Values; }
        }

        public TValue this[TKey key] {
            get {
                return _dictionary[key];
            }
            set {
                throw new NotSupportedException("This dictionary is read-only");
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        public void Add(KeyValuePair<TKey, TValue> item) {
            throw new NotSupportedException("This dictionary is read-only");
        }

        public void Clear() {
            throw new NotSupportedException("This dictionary is read-only");
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) {
            return _dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            _dictionary.CopyTo(array, arrayIndex);
        }

        public int Count {
            get { return _dictionary.Count; }
        }

        public bool IsReadOnly {
            get { return true; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) {
            throw new NotSupportedException("This dictionary is read-only");
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            return _dictionary.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return (_dictionary as System.Collections.IEnumerable).GetEnumerator();
        }

        #endregion
    }
}
