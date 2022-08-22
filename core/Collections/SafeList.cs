// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ImageResizer.Collections
{
    /// <summary>
    ///     SafeList is mutable, but it uses immutable data structures to minimize the need for locking.
    ///     The provided manipulation exposes an immutable list. Changes are made by copying the lists.
    ///     Never perform logic on SafeList directly, always use GetList() or GetCollection() first, followed by SetList().
    ///     If you need involved list-fu, use ModifyList and specify a callback. It will execute inside a lock, preventing
    ///     changes on other threads from overwriting each other.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SafeList<T> : IEnumerable<T>
    {
        public delegate void ChangedHandler(SafeList<T> sender);

        public delegate IEnumerable<T> ListEditor(IList<T> items);

        [CLSCompliant(false)] protected volatile ReadOnlyCollection<T> items;

        protected object writeLock = new object();


        public SafeList()
        {
            items = new ReadOnlyCollection<T>(new List<T>());
        }

        public SafeList(IEnumerable<T> items)
        {
            items = new ReadOnlyCollection<T>(new List<T>(items));
        }

        public event ChangedHandler Changed;

        protected void FireChanged()
        {
            if (Changed != null) Changed(this);
        }

        /// <summary>
        ///     Returns an immutable snapshot of the collection
        /// </summary>
        /// <returns></returns>
        public ReadOnlyCollection<T> GetCollection()
        {
            return items;
        }

        /// <summary>
        ///     Returns a mutable snapshot of the list
        /// </summary>
        /// <returns></returns>
        public IList<T> GetList()
        {
            return new List<T>(items);
        }

        /// <summary>
        ///     Replaces the current collection with a new one. (copied to ensure safety)
        ///     Use ModifyList when modifying the list. Use this only when the previous or current state of the list is irrelevant.
        /// </summary>
        /// <param name="list"></param>
        public void SetList(IEnumerable<T> list)
        {
            lock (writeLock)
            {
                items = new ReadOnlyCollection<T>(new List<T>(list));
            }

            FireChanged();
        }

        /// <summary>
        ///     Adds the specified item to the end of the list
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            lock (writeLock)
            {
                var newList = GetList();
                newList.Add(item);
                items = new ReadOnlyCollection<T>(newList);
            }

            FireChanged();
        }

        /// <summary>
        ///     Removes the item from the list
        /// </summary>
        /// <param name="item"></param>
        public bool Remove(T item)
        {
            lock (writeLock)
            {
                var newList = GetList();
                var removed = newList.Remove(item);
                if (!removed) return false; //The item didn't exist, don't fire changed events.
                items = new ReadOnlyCollection<T>(newList);
            }

            FireChanged();
            return true;
        }

        /// <summary>
        ///     Returns the first item in the list. May return null if the list is empty.
        /// </summary>
        public T First
        {
            get
            {
                var copy = items; //So we can do logic without getting an index invalid exception
                if (copy.Count > 0) return copy[0];
                else return default;
            }
        }

        /// <summary>
        ///     Returns the first item in the list. May return null if the list is empty.
        /// </summary>
        public T Last
        {
            get
            {
                var copy = items; //So we can do logic without getting an index invalid exception
                if (copy.Count > 0) return copy[copy.Count - 1];
                else return default;
            }
        }

        /// <summary>
        ///     Adds the specified item to the beginning of the list
        /// </summary>
        /// <param name="item"></param>
        public void AddFirst(T item)
        {
            lock (writeLock)
            {
                var newList = GetList();
                newList.Insert(0, item);
                items = new ReadOnlyCollection<T>(newList);
            }

            FireChanged();
        }


        /// <summary>
        ///     Allows a caller to perform logic on the list inside a lock, and return a modified list.
        ///     Callbacks should be fast, and should reference the IList they are fed, not this SafeList instance.
        ///     Calling methods on the SafeList instance will cause a deadlock.
        /// </summary>
        /// <param name="callback"></param>
        public void ModifyList(ListEditor callback)
        {
            lock (writeLock)
            {
                items = new ReadOnlyCollection<T>(new List<T>(callback(GetList())));
            }

            FireChanged();
        }

        /// <summary>
        ///     Returns true if the collection contains the specified item at the moment.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            return items.Contains(item);
        }


        public IEnumerator<T> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)items).GetEnumerator();
        }

        public IEnumerable<T> Reversed => new ReverseEnumerable<T>(items);
    }
}