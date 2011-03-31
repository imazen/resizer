using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Collections.Specialized;
using System.Xml;
using fbs.ImageResizer.Configuration;
using System.Xml.XPath;

namespace fbs.ImageResizer.Configuration {

    /// <summary>
    /// Handles reading the imageresizer section from Web.Config
    /// </summary>
    public class ResizerConfigurationSection : ConfigurationSection {

        public string getAttr(string selector, string defaultValue) {

            //Convert to XPath
            // pipeline.fakeExtension becomes
            // /resizer/pipeline/@fakeExtension
               
            string s = selector;
            int lastDot = s.LastIndexOf('.');
            if (lastDot < 0) throw new ArgumentException("Selector must specify the attribute name, in the form element.child.attribute");

            //Fix the attribute selector
            s = s.Substring(0, lastDot) + "/@" + s.Substring(lastDot + 1);
            //Trim leading dots
            s = s.TrimStart('.');
            //Convert dots
            s = s.Replace('.', '/');
            //Prepend document selector
            s = "/resizer/" + s;

            lock (docSync) {
                initDoc();
                XmlNode n = doc.SelectSingleNode(s);
                return n.Value;
            }

        }


        /// <summary>
        /// Get/set configuration values in the form element.attribute, as direct children of the imageresizer configuration section.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>


        protected object docSync = new object();
        protected volatile XmlDocument doc = null;
        /// <summary>
        /// Initializes the 'doc' XmlDocument with a root node named 'resizer'
        /// </summary>
        protected void initDoc() {
            if (doc != null) return;
            doc = new XmlDocument();
            //Create xml declaration
            doc.InsertBefore(doc.CreateXmlDeclaration("1.0", "utf-8", null), doc.DocumentElement);
            //Root node.
            doc.AppendChild(doc.CreateElement("resizer"));
            
        }
        /// <summary>
        /// Called for each child element not specified declaratibely
        /// </summary>
        /// <param name="elementName"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        protected override bool OnDeserializeUnrecognizedElement(string elementName, System.Xml.XmlReader reader) {
            lock(docSync){
                initDoc(); //Verify the doc is initialized
                //Add it under the root <resizer> node.
                XmlNode n = doc.ReadNode(reader);
                doc.DocumentElement.AppendChild(n);
            }
             return true;
        }




    }
}
