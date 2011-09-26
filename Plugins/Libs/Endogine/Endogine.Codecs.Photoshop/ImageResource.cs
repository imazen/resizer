/*
* Copyright (c) 2006, Jonas Beckeman
* All rights reserved.
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*
*     * Redistributions of source code must retain the above copyright
*       notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright
*       notice, this list of conditions and the following disclaimer in the
*       documentation and/or other materials provided with the distribution.
*     * Neither the name of Jonas Beckeman nor the names of its contributors
*       may be used to endorse or promote products derived from this software
*       without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY JONAS BECKEMAN AND CONTRIBUTORS ``AS IS'' AND ANY
* EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
* WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
* DISCLAIMED. IN NO EVENT SHALL JONAS BECKEMAN AND CONTRIBUTORS BE LIABLE FOR ANY
* DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
* (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
* LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
* ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
* SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*
* HEADER_END*/

using System;

using System.Collections.Generic;
using System.Xml.Serialization;

namespace Endogine.Codecs.Photoshop
{
    [XmlInclude(typeof(ImageResources.AlphaChannelNames))]
    [XmlInclude(typeof(ImageResources.AlphaIdentifiers))]
    [XmlInclude(typeof(ImageResources.ColorHalftoneInfo))]
    [XmlInclude(typeof(ImageResources.ColorTransferFunctions))]
    [XmlInclude(typeof(ImageResources.CopyrightInfo))]
    [XmlInclude(typeof(ImageResources.DisplayInfo))]
    [XmlInclude(typeof(ImageResources.DocumentSpecificIds))]
    [XmlInclude(typeof(ImageResources.GlobalAngle))]
    [XmlInclude(typeof(ImageResources.GlobalAltitude))]
    [XmlInclude(typeof(ImageResources.GridGuidesInfo))]
    [XmlInclude(typeof(ImageResources.IPTC_NAA))]
    [XmlInclude(typeof(ImageResources.ICCUntagged))]
    [XmlInclude(typeof(ImageResources.LayersGroupInfo))]
    [XmlInclude(typeof(ImageResources.LayerStateInfo))]
    [XmlInclude(typeof(ImageResources.PathInfo))]
    [XmlInclude(typeof(ImageResources.PathInfo.BezierKnot))]
    [XmlInclude(typeof(ImageResources.PathInfo.Clipboard))]
    [XmlInclude(typeof(ImageResources.PathInfo.NewPath))]
    [XmlInclude(typeof(ImageResources.PrintFlags))]
    [XmlInclude(typeof(ImageResources.PrintFlagsInfo))]
    [XmlInclude(typeof(ImageResources.ResolutionInfo))]
    [XmlInclude(typeof(ImageResources.Slices))]
    [XmlInclude(typeof(ImageResources.Thumbnail))]
    [XmlInclude(typeof(ImageResources.UnicodeAlphaNames))]
    [XmlInclude(typeof(ImageResources.URL))]
    [XmlInclude(typeof(ImageResources.URLList))]
    [XmlInclude(typeof(ImageResources.VersionInfo))]
    [XmlInclude(typeof(ImageResources.XMLInfo))]
    public class ImageResource
	{
        public enum ResourceIDs
        {
            Undefined = 0,
            MacPrintInfo = 1001,
            ResolutionInfo = 1005,
            AlphaChannelNames = 1006,
            DisplayInfo = 1007,
            Caption = 1008,
            BorderInfo = 1009,
            BgColor = 1010,
            PrintFlags = 1011, //1-byte values labels, crop marks, color bars, registration marks, negative, flip, interpolate, caption.
            MultiChannelHalftoneInfo = 1012,
            ColorHalftoneInfo = 1013,
            DuotoneHalftoneInfo = 1014,
            MultiChannelTransferFunctions = 1015,
            ColorTransferFunctions = 1016,
            DuotoneTransferFunctions = 1017,
            DuotoneImageInfo = 1018,
            BlackWhiteRange = 1019,
            EPSOptions = 1021,
            QuickMaskInfo = 1022, //2 bytes containing Quick Mask channel ID, 1 byte boolean indicating whether the mask was initially empty.
            LayerStateInfo = 1024, //2 bytes containing the index of target layer. 0=bottom layer.
            WorkingPathUnsaved = 1025,
            LayersGroupInfo = 1026,
            IPTC_NAA = 1028,
            RawFormatImageMode = 1029,
            JPEGQuality = 1030,
            GridGuidesInfo = 1032,
            Thumbnail1 = 1033,
            CopyrightInfo = 1034,
            URL = 1035,
            Thumbnail2 = 1036,
            GlobalAngle = 1037,
            ColorSamplers = 1038,
            ICCProfile = 1039, //The raw bytes of an ICC format profile, see the ICC34.pdf and ICC34.h files from the Internation Color Consortium located in the documentation section
            Watermark = 1040,
            ICCUntagged = 1041, //1 byte that disables any assumed profile handling when opening the file. 1 = intentionally untagged.
            EffectsVisible = 1042, //1 byte global flag to show/hide all the effects layer. Only present when they are hidden.
            SpotHalftone = 1043, // 4 bytes for version, 4 bytes for length, and the variable length data.
            DocumentSpecificIds = 1044,
            UnicodeAlphaNames = 1045,
            IndexedColorTableCount = 1046, // 2 bytes for the number of colors in table that are actually defined
            TransparentIndex = 1047,
            GlobalAltitude = 1049,  // 4 byte entry for altitude
            Slices = 1050,
            WorkflowURL = 1051, //Unicode string, 4 bytes of length followed by unicode string
            JumpToXPEP = 1052, //2 bytes major version, 2 bytes minor version,
            //4 bytes count. Following is repeated for count: 4 bytes block size,
            //4 bytes key, if key = 'jtDd' then next is a Boolean for the dirty flag
            //otherwise its a 4 byte entry for the mod date
            AlphaIdentifiers = 1053, //4 bytes of length, followed by 4 bytes each for every alpha identifier.
            URLList = 1054, //4 byte count of URLs, followed by 4 byte long, 4 byte ID, and unicode string for each count.
            VersionInfo = 1057,
            Unknown4 = 1058, //pretty long, 302 bytes in one file. Holds creation date, maybe Photoshop license number
            XMLInfo = 1060,
            Unknown = 1061, //seems to be common!
            Unknown2 = 1062, //seems to be common!
            Unknown3 = 1064, //seems to be common!
            PathInfo = 2000, //2000-2999 actually I think?
            ClippingPathName = 2999,
            PrintFlagsInfo = 10000 //2 bytes version (=1), 1 byte center crop marks, 1 byte (=0), 4 bytes bleed width value, 2 bytes bleed width scale
        }

        [XmlIgnoreAttribute()]
        public ushort ID;
        [XmlAttributeAttribute()]
        public ResourceIDs ResIdForTempXml
        {
            get { return (ResourceIDs)ID; }
            set { }
        }
        [XmlIgnoreAttribute()]
        public string Name; //Doesn't seem to be used..?
        [XmlIgnoreAttribute()]
		public byte[] Data;
        public string DataForXml
        {
            get
            {
                if (this.Data == null)
                    return null;
                return Endogine.Serialization.ReadableBinary.CreateHexEditorString(this.Data);
            }
            set { }
        }

        [XmlIgnoreAttribute()]
        public static Dictionary<ResourceIDs, Type> ResourceTypes;

		public ImageResource()
		{
		}
        
		public ImageResource(ImageResource imgRes)
		{
			this.ID = imgRes.ID;
			this.Name = imgRes.Name;
		}

		public ImageResource(BinaryPSDReader reader)
		{
			this.ID = reader.ReadUInt16();
			this.Name = reader.ReadPascalString();
			uint settingLength = reader.ReadUInt32();
			this.Data = reader.ReadBytes((int)settingLength);
			if (reader.BaseStream.Position % 2 == 1)
				reader.ReadByte();
		}

		public BinaryPSDReader GetDataReader()
		{
			System.IO.MemoryStream stream = new System.IO.MemoryStream(this.Data);
			return new BinaryPSDReader(stream);
		}

        public virtual ResourceIDs[] AcceptedResourceIDs
        {
            get { return null; }
        }
        //public string GetIdAsFOURCC()
        //{
        //    return "" + (char)((this.ID >> 24) & 0xff) + (char)((this.ID >> 16) & 0xff) + (char)((this.ID >> 8) & 0xff) + (char)(this.ID & 0xff);
        //}

        public void Write(BinaryPSDWriter writer)
        {
            writer.Write("8BIM");
            writer.Write(this.ID);
            writer.WritePascalString(this.Name);
            writer.StartLengthBlock(typeof(uint));
            this.SubWrite(writer);
            writer.EndLengthBlock();
            writer.PadToNextMultiple(2);
            //if (writer.GetCurrentBlockLength() % 2 == 1)
            //    writer.Write((byte)0);
        }

        protected virtual void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write(this.Data);
        }



        public static void Prepare()
        {
            if (ResourceTypes != null)
                return;

            //Get ImageResource types
            //TODO: is there a way to not get *all* types, but only those in a sub-namespace?

            ResourceTypes = new Dictionary<ResourceIDs, Type>();
            Type[] types = typeof(ImageResource).Assembly.GetTypes();
            foreach (Type type in types)
            {
                if (type.FullName.Contains("ImageResources."))
                {
                    if (type.FullName.Contains("+"))
                        continue;
                    Type actualType = type; // type.ReflectedType; //Because strangely, when type has internal classes, it wraps both of them somehow? e.g. GridGuidesInfo+GridGuide
                    System.Reflection.ConstructorInfo ci = actualType.GetConstructor(new Type[] { });
                    ImageResource ir = (ImageResource)ci.Invoke(new object[] { });
                    ResourceIDs[] rids = ir.AcceptedResourceIDs;
                    if (rids == null)
                    {
                        string name = actualType.FullName.Substring(type.FullName.LastIndexOf(".") + 1);
                        rids = new ResourceIDs[] { (ResourceIDs)Enum.Parse(typeof(ResourceIDs), name) };
                    }
                    foreach (ResourceIDs rid in rids)
                    {
                        ResourceTypes.Add(rid, actualType);
                    }
                }
            }
        }
        public static List<ImageResource> ReadImageResources(BinaryPSDReader reader)
        {
            List<ImageResource> result = new List<ImageResource>();
            while (true)
            {
                long nBefore = reader.BaseStream.Position;
                string settingSignature = new string(reader.ReadPSDChars(4));
                if (settingSignature != "8BIM")
                {
                    reader.BaseStream.Position = nBefore;
                    //reader.BaseStream.Position-=4;
                    break;
                }

                ImageResource imgRes = new ImageResource(reader);
                ResourceIDs resID = (ResourceIDs)imgRes.ID;
                if (!Enum.IsDefined(typeof(ResourceIDs), (int)imgRes.ID))
                {
                    if (imgRes.ID > 2000 && imgRes.ID <= 2999)
                    {
                        //Stupid Adobe engineers... This is SO not using the same pattern as everything else!!!
                        resID = ResourceIDs.PathInfo;
                    }
                }

                if (ResourceTypes.ContainsKey(resID))
                {
                    Type type = ResourceTypes[resID];
                    System.Reflection.ConstructorInfo ci = type.GetConstructor(new Type[] { typeof(ImageResource) });
                    imgRes = (ImageResource)ci.Invoke(new object[] { imgRes });
                }
                //if (resID != ResourceIDs.Undefined)
                result.Add(imgRes);
            }
            return result;
        }

        public static ImageResource CreateResource(Type type)
        {
            System.Reflection.ConstructorInfo ci = type.GetConstructor(new Type[] { });
            return (ImageResource)ci.Invoke(new object[] { });
        }
	}
}
