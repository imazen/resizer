using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Collections.Specialized;
using System.Xml;
using fbs.ImageResizer.Configuration;

namespace fbs.ImageResizer.Configuration {

    /// <summary>
    /// Handles reading the imageresizer section from Web.Config
    /// </summary>
    public class ResizerConfigurationSection : ConfigurationSection {
        
        /// <summary>
        /// Whether images are accessed directly from the file system or through virtual path providers.
        /// </summary>
        [ConfigurationProperty("vppUsage", IsRequired = false, DefaultValue = VppUsageOption.Fallback)]
        public VppUsageOption VppUsage {
            get {
                return (VppUsageOption)base["vppUsage"];
            }
            set {
                base["vppUsage"] = value;
            }
        }

        /// <summary>
        /// Not thread-safe - use public methods
        /// </summary>
        protected Dictionary<string, NameValueCollection> elements = new Dictionary<string, NameValueCollection>(StringComparer.OrdinalIgnoreCase);

        private object elementsLock = new object();
        public string get(string selector) {
            return get(selector, null);
        }
        public string get(string selector, string defaultValue) {
            int dot = selector.IndexOf('.'); 
            string element = selector;
            string attribute = null;
            if (dot > -1) {
                element = element.Substring(0, dot);
                attribute = selector.Substring(dot + 1);
            }
            if (attribute == null) throw new ArgumentException("Selector must contain an element AND attribute value in the form 'element.attribute'. Received: " + selector);

            //Lock all access
            lock (elementsLock) {
                //If the element doesn't exist, return defaultValue.
                if (!elements.ContainsKey(element)) return defaultValue; 
                //Load value
                string val = elements[element][attribute];
                //If the value wasn't specified, return defaultValue
                if (val == null) return defaultValue;
                //Return the value successfully
                return val;
            }
        }
        public void set(string selector, string value) {
            int dot = selector.IndexOf('.');
            string element = selector;
            string attribute = null;
            if (dot > -1) {
                element = element.Substring(0, dot);
                attribute = selector.Substring(dot + 1);
            }
            if (attribute == null) throw new ArgumentException("Selector must contain an element AND attribute value in the form 'element.attribute'. Received: " + selector);

            //Lock all access
            lock (elementsLock) {
                //If the element doesn't exist, create it
                if (!elements.ContainsKey(element)) elements.Add(element, new NameValueCollection());
                //Set value
                elements[element][attribute] = value;
            }
        }

        /// <summary>
        /// Get/set configuration values in the form element.attribute, as direct children of the imageresizer configuration section.
        /// </summary>
        /// <param name="selector"></param>
        /// <returns></returns>
        public string this[string selector] {
            get {
                return get(selector);
            }
            set {
                set(selector, value);
            }
        }

        

        protected override bool OnDeserializeUnrecognizedElement(string elementName, System.Xml.XmlReader reader) {
            NameValueCollection attrs = new NameValueCollection();

            // check to see if the current node has attributes
             if (reader.HasAttributes)
             {
                // move to the first attribute
                reader.MoveToFirstAttribute();

                // enumerate all attributes
                while (reader.MoveToNextAttribute())
                {
                    attrs[reader.Name] = reader.Value; 
                }

                // move back to the element node that contains
                // the attributes we just traversed
                reader.MoveToElement();
                reader.Skip();
             }
             elements.Add(elementName, attrs);
             return true;
        }




    }
}
