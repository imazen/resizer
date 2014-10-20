/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Collections.Specialized;
using System.Xml;
using ImageResizer.Configuration;
using System.Xml.XPath;
using ImageResizer.Configuration.Xml;
using ImageResizer.Configuration.Issues;

namespace ImageResizer {

    /// <summary>
    /// Handles reading the &lt;resizer&gt; section from Web.Config
    /// </summary>
    public class ResizerSection : System.Configuration.ConfigurationSection {
        public ResizerSection() {
        }
        /// <summary>
        /// Create a ResizerSection instance that wraps the specified node. The node should be a &lt;resizer&gt; element.
        /// </summary>
        /// <param name="root"></param>
        public ResizerSection(Node root) {
            n = root;
        }
        /// <summary>
        /// Parse the specified XML into a Node. The text should include 1 root element, &lt;resizer&gt;
        /// </summary>
        /// <param name="xml"></param>
        public ResizerSection(string xml) {

            n = Node.FromXmlFragment(xml,sink);
        }
        protected object nSync = new object();

        [CLSCompliant(false)]
        protected volatile Node n = new Node("resizer");

        [CLSCompliant(false)]
        protected volatile XmlDocument xmlDoc = new XmlDocument();


        /// <summary>
        /// Returns the specified subtree, deep copied so it can be used without locking.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public Node getCopyOfNode(string selector) {
            lock(nSync){
                Node r = n.queryFirst(selector);
                return (r != null) ? r.deepCopy() : null;
            }
        }

        public Node getCopyOfRootNode() {
            lock (nSync) {
                return n.deepCopy();
            }
        }
        public void replaceRootNode(Node n) {
            lock (nSync) {
                this.n = n;
            }
        }
        public string getAttr(string selector, string defaultValue) {
            lock (nSync) {
                string v = n.queryAttr(selector);
                return v != null ? v : defaultValue;
            }
        }
        public void setAttr(string selector, string value) {
            lock (nSync) {
                n.setAttr(selector, value);
            }
        }


        /// <summary>
        /// Called for each child element not specified declaratively
        /// </summary>
        /// <param name="elementName"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        protected override bool OnDeserializeUnrecognizedElement(string elementName, System.Xml.XmlReader reader) {
            lock(nSync){
                n.Children.Add(new Node(xmlDoc.ReadNode(reader) as XmlElement, sink));
            }
            return true;
        }

        protected override bool OnDeserializeUnrecognizedAttribute(string name, string value) {
            n.Attrs[name] = value;
            return true;
        }

        protected override bool SerializeToXmlElement(XmlWriter writer, string elementName) {
            if (n.IsEmpty) return false;
            XmlElement e = null;
            lock (nSync) e = n.ToXmlElement();
            writer.WriteRaw(e.OuterXml);
            return true;
        }

        protected IssueSink sink = new IssueSink("resizer configuration");
        public IssueSink IssueSink { get { return sink; } }

    }
}
