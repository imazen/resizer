// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Xml;
using Imazen.Common.Issues;

namespace ImageResizer.Configuration.Xml
{
    /// <summary>
    ///     No support for namespaces, no intention of eventual serialization.
    ///     Everything is case-insensitive, but preserves case. Not thread safe.
    /// </summary>
    public class Node
    {
        public Node(string localName)
        {
            name = localName;
        }

        /// <summary>
        ///     Builds a tree of Nodes from the specified XML subtree. Duplicate attributes are sent to 'ir'
        /// </summary>
        /// <param name="e"></param>
        /// <param name="ir"></param>
        public Node(XmlElement e, IIssueReceiver ir)
        {
            name = e.LocalName;
            //Copy attributes, raising an issue if duplicates are found
            foreach (XmlAttribute a in e.Attributes)
            {
                if (attrs[a.LocalName] != null)
                    ir.AcceptIssue(new Issue("Two or more attributes named " + a.LocalName + " found on element " +
                        name + " in "
                        + e.ParentNode != null
                            ? e.ParentNode.Name
                            : "(unknown node)"));
                attrs[a.LocalName] = a.Value;
            }

            //Parse children
            if (e.HasChildNodes)
            {
                StringBuilder sb = null;
                foreach (XmlNode n in e.ChildNodes)
                    if (n.NodeType == XmlNodeType.Element)
                    {
                        var child = n as XmlElement;
                        if (child != null) children.Add(new Node(child, ir));
                        //Collect text and whitespace
                    }
                    else if (n.NodeType == XmlNodeType.Text || n.NodeType == XmlNodeType.EntityReference ||
                             n.NodeType == XmlNodeType.SignificantWhitespace)
                    {
                        //|| n.NodeType == XmlNodeType.Whitespace
                        if (sb == null) sb = new StringBuilder();
                        sb.Append(n.Value);
                    }

                //Save text/whitespace
                if (sb != null) TextContents = sb.ToString();
            }
        }

        private NameValueCollection attrs = new NameValueCollection();

        /// <summary>
        ///     Attributes
        /// </summary>
        public NameValueCollection Attrs
        {
            get => attrs;
            set => attrs = value;
        }

        /// <summary>
        ///     Access attributes by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string this[string name]
        {
            get => attrs[name];
            set => attrs[name] = value;
        }

        /// <summary>
        ///     The concatenated text, significant whitespace, and entity references within this element.
        ///     Not XML compliant for sure.
        /// </summary>
        public string TextContents { get; set; }


        private string name = null;

        /// <summary>
        ///     The name of the element.
        /// </summary>
        public string Name
        {
            get => name;
            set => name = value;
        }

        private List<Node> children = new List<Node>();

        /// <summary>
        ///     Child nodes
        /// </summary>
        public List<Node> Children
        {
            get => children;
            set => children = value;
        }

        /// <summary>
        ///     Returns the subset of Children with a matching element name. (Case-insensitive)
        /// </summary>
        /// <param name="elementName"></param>
        /// <returns></returns>
        public IList<Node> childrenByName(string elementName)
        {
            var results = new List<Node>();
            if (children == null || children.Count == 0) return results;
            foreach (var n in children)
                if (n.Name.Equals(elementName, StringComparison.OrdinalIgnoreCase))
                    results.Add(n);

            return results;
        }

        /// <summary>
        ///     Queries the subtree for the specified attribute on the specified element. Example selector:
        ///     element.element.attrname
        ///     Assumes that the last segment of the selector is an attribute name.
        ///     Throws an ArgumentException if there is only one segment ( element ).
        ///     Uses the cache.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public string queryAttr(string selector)
        {
            var parsed = parseAttributeName(selector);
            return queryAttr(parsed.Key, parsed.Value);
        }

        protected KeyValuePair<string, string> parseAttributeName(string selector)
        {
            selector = selector.Trim('.');
            var lastDot = selector.LastIndexOf('.');
            if (lastDot < 0)
                throw new ArgumentException(
                    "Selector must include an attribute name, like element.attrname. Was given '" + selector + "'");
            var nodeSelector = selector.Substring(0, lastDot);
            var attrName = selector.Substring(lastDot + 1);
            return new KeyValuePair<string, string>(nodeSelector, attrName);
        }

        public string queryAttr(string nodeSelector, string attrName)
        {
            var n = queryFirst(nodeSelector);
            if (n != null) return n.Attrs[attrName];
            return null;
        }

        public Node queryFirst(string selector)
        {
            var results = query(selector);
            if (results != null && results.Count > 0)
                foreach (var n in results)
                    return n; //Return the first node.
            return null;
        }

        /// <summary>
        ///     Sets the specified attribute value, creating parent elements if needed. Clears the query cache.
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="attrValue"></param>
        public void setAttr(string selector, string attrValue)
        {
            var parsed = parseAttributeName(selector);
            setAttr(parsed.Key, parsed.Value, attrValue);
        }

        /// <summary>
        ///     Sets the specified attribute value, creating parent elements if needed. Clears the query cache.
        /// </summary>
        /// <param name="nodeSelector"></param>
        /// <param name="attrName"></param>
        /// <param name="attrValue"></param>
        public void setAttr(string nodeSelector, string attrName, string attrValue)
        {
            var n = queryFirst(nodeSelector);
            if (n == null) n = makeNodeTree(nodeSelector);
            n.Attrs[attrName] = attrValue;
            clearQueryCache();
        }

        /// <summary>
        ///     Traverses the specified path, creating any missing elements along the way. Uses existing nodes if found.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public Node makeNodeTree(string selector)
        {
            var n = this;
            var s = new Selector(selector);
            foreach (var part in s)
            {
                var results = n.childrenByName(part);
                //Add it if doesn't exist
                if (results == null || results.Count == 0)
                {
                    var newNode = new Node(part);
                    if (n.children == null) n.children = new List<Node>();
                    n.Children.Add(newNode);
                    n = newNode;
                }
                else
                {
                    n = results[0];
                }
            }

            return n;
        }

        protected Dictionary<string, ICollection<Node>> _cachedResults =
            new Dictionary<string, ICollection<Node>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Same as query(), except results are cached until clearQueryCache() is called.
        ///     Faster, but can be incorrect if existing nodes are renamed, moved, or deleted.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public ICollection<Node> query(string selector)
        {
            if (_cachedResults.ContainsKey(selector)) return _cachedResults[selector];

            var uncachedResults = queryUncached(selector) as IList<Node>;
            //cache miss
            var results = _cachedResults[selector] =
                uncachedResults != null ? new ReadOnlyCollection<Node>(uncachedResults) : null;
            return results;
        }

        public void clearQueryCache()
        {
            _cachedResults = new Dictionary<string, ICollection<Node>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Mutates this tree to redact all the matching attributes
        /// </summary>
        /// <param name="elementSelector"></param>
        /// <param name="attributeNames"></param>
        /// <param name="replaceWith"></param>
        /// <returns></returns>
        public Node RedactAttributes(string elementSelector, string[] attributeNames, string replaceWith = "[redacted]")
        {
            foreach (var n in queryUncached(elementSelector) ?? Enumerable.Empty<Node>())
            foreach (var attrName in attributeNames)
                if (n.Attrs[attrName] != null)
                    n.Attrs.Set(attrName, replaceWith);
            return this;
        }

        public ICollection<Node> queryUncached(string selector)
        {
            if (children == null || children.Count == 0) return null;

            selector = selector.Trim('.'); //Trim leading and trailing dots

            var nextDot = selector.IndexOf('.');
            //Get the first item
            var nextBit = nextDot > -1 ? selector.Substring(0, nextDot) : selector;
            //Get the remainder of the query
            var remainder = nextDot > -1 ? selector.Substring(nextDot + 1) : null;


            List<Node> results = null;
            foreach (var n in children)
                if (n.Name.Equals(nextBit, StringComparison.OrdinalIgnoreCase))
                {
                    //If this is the last part of the query, add results directly
                    if (string.IsNullOrEmpty(remainder))
                    {
                        if (results == null) results = new List<Node>();
                        results.Add(n);
                    }
                    else
                    {
                        //Execute subquery and add results
                        var subQueryResults = n.queryUncached(remainder);
                        if (subQueryResults != null)
                        {
                            if (results == null) results = new List<Node>();
                            results.AddRange(subQueryResults);
                        }
                    }
                }

            return results;
        }

        /// <summary>
        ///     Makes a recursive copy of the subtree, keeping no duplicate references to mutable types.
        /// </summary>
        /// <returns></returns>
        public Node deepCopy()
        {
            var n = new Node(name);
            //copy attrs
            foreach (string key in Attrs.Keys) n[key] = this[key];

            //copy children recursive
            if (children != null)
                foreach (var c in Children)
                    n.Children.Add(c.deepCopy());
            //Copy text contents
            n.TextContents = TextContents;

            return n;
        }

        /// <summary>
        ///     Returns true if the element has neither attributes nor children.
        /// </summary>
        public bool IsEmpty => (attrs == null || attrs.Count == 0) && (children == null || children.Count == 0);

        public XmlElement ToXmlElement()
        {
            return ToXmlElement(new XmlDocument());
        }

        public XmlElement ToXmlElement(XmlDocument doc)
        {
            var e = doc.CreateElement(Name);
            //Copy attrs
            if (attrs != null)
                foreach (string key in attrs)
                {
                    var a = doc.CreateAttribute(key);
                    a.Value = this[key];
                    e.Attributes.Append(a);
                }

            //Copy children.
            if (children != null)
                foreach (var c in children)
                    e.AppendChild(c.ToXmlElement(doc));
            //Copy text
            if (TextContents != null) e.AppendChild(doc.CreateTextNode(TextContents));

            return e;
        }

        public override string ToString()
        {
            return ToXmlElement(new XmlDocument()).OuterXml.Replace(">", ">\n");
        }

        public static Node FromXmlFragment(string xml, IssueSink sink)
        {
            var nt = new NameTable();

            var nsmanager = new XmlNamespaceManager(nt);
            var context =
                new XmlParserContext(nt, nsmanager, "elem", XmlSpace.None, System.Text.Encoding.UTF8);
            var reader = new XmlTextReader(xml, XmlNodeType.Element, context);


            var n = new Node(new XmlDocument().ReadNode(reader) as XmlElement, sink);
            reader.Close();
            return n;
        }
    }
}