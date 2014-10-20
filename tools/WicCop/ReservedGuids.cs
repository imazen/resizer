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
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.Test.Tools.WicCop
{
    public class ReservedGuids
    {
        public struct ReservedGuid
        {
            [XmlAttribute]
            public Guid guid;

            [XmlAttribute]
            public string description;
        }

        readonly List<ReservedGuid> items = new List<ReservedGuid>();

        readonly public static ReservedGuids Instance = Read();

        [XmlElement("Item")]
        public List<ReservedGuid> Items
        {
            get { return items; }
        }

        public bool TryGetValue(Guid guid, out string value)
        {
            foreach (ReservedGuid i in items)
            {
                if (i.guid == guid)
                {
                    value = i.description;

                    return true;
                }
            }

            value = null;

            return false;
        }

        static ReservedGuids Read()
        {
            Type thisType = typeof(ReservedGuids);

            XmlSerializer xs = new XmlSerializer(thisType);

            foreach (string s in thisType.Assembly.GetManifestResourceNames())
            {
                if (s.EndsWith(thisType.Name + ".xml"))
                {
                    using (Stream stream = thisType.Assembly.GetManifestResourceStream(s))
                    {
                        return xs.Deserialize(stream) as ReservedGuids;
                    }
                }
            }

            throw new FileNotFoundException();
        }
    }
}
