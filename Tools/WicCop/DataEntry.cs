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
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;

using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using Microsoft.Test.Tools.WicCop.Properties;

namespace Microsoft.Test.Tools.WicCop
{
    [Serializable]
    public struct DataEntry
    {
        readonly static Dictionary<Guid, string> knownGuids = GetKnown();

        readonly string text;
        readonly object value;

        public DataEntry(string text, object value)
        {
            this.text = text;
            this.value = value;
            if (value != null)
            {
                if (value.GetType() == typeof(Guid))
                {
                    this.value = GetValue((Guid)value);
                }
                else if (value.GetType() == typeof(Guid[]))
                {
                    this.value = Array.ConvertAll<Guid, object>(value as Guid[], GetValue);
                }
            }
        }

        public DataEntry(Exception e)
            : this("HRESULT", GetValue(e))
        {
        }

        public DataEntry(WinCodecError error)
            : this("HRESULT", error)
        {
        }

        public DataEntry(string text, Exception e)
            : this(text, GetValue(e))
        {
        }

        public DataEntry(string file)
            : this(Resources.File, file)
        {
        }

        public DataEntry(uint frameIndex)
            : this(Resources.FrameIndex, frameIndex)
        {
        }

        static Dictionary<Guid, string> GetKnown()
        {
            Dictionary<Guid, string> res = new Dictionary<Guid, string>();

            foreach (ReservedGuids.ReservedGuid r in ReservedGuids.Instance.Items)
            {
                res[r.guid] = r.description;
            }

            foreach (FieldInfo fi in typeof(Consts).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (fi.FieldType == typeof(Guid))
                {
                    res[(Guid)fi.GetValue(null)] = fi.Name;
                }
            }

            return res;
        }

        static object GetValue(Guid value)
        {
            string res;

            if (knownGuids.TryGetValue(value, out res))
            {
                return res;
            }
            else
            {
                return value;
            }
        }

        static object GetValue(Exception value)
        {
            int hr = Marshal.GetHRForException(value);

            if (Enum.IsDefined(typeof(WinCodecError), hr))
            {
                return (WinCodecError)hr;
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, "0x{0:X}", hr);
            }
        }

        public string Text
        {
            get { return text; }
        }

        public object Value
        {
            get { return value; }
        }

        public void WriteTo(XmlWriter xw)
        {
            xw.WriteStartElement("Entry");
            xw.WriteAttributeString("text", Text);
            Array a = Value as Array;
            if (a == null)
            {
                a = new object[] { Value };
            }
            foreach (object o in a)
            {
                xw.WriteStartElement("Value");
                if (o != null)
                {
                    xw.WriteValue(o.ToString());
                }
                xw.WriteEndElement();
            }
            xw.WriteEndElement();
        }
    }
}
