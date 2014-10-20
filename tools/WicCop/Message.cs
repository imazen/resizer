//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;

using Microsoft.Test.Tools.WicCop.Rules;

namespace Microsoft.Test.Tools.WicCop
{
    class Message : ListViewItem
    {
        readonly List<DataEntryCollection> data = new List<DataEntryCollection>();
        readonly RuleBase parent;

        public Message(RuleBase parent, string text, params DataEntry[][] data)
            : base (text)
        {
            this.parent = parent;
            this.data.Add(new DataEntryCollection(parent, data));

            SubItems.Add(parent.FullPath);
        }

        public RuleBase Parent
        {
            get { return parent; }
        }

        public List<DataEntryCollection> Data
        {
            get { return data; }
        }

        public void WriteTo(XmlWriter xw)
        {
            xw.WriteStartElement("Message");
            xw.WriteAttributeString("text", Text);
            xw.WriteAttributeString("node", parent.FullPath);
            foreach (DataEntryCollection c in Data)
            {
                c.WriteTo(xw);
            }
            xw.WriteEndElement();
        }
    }
}
