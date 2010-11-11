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
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using PhotoshopFile;
using System.Drawing;
using System.Diagnostics;

namespace PhotoshopFile.Text
{
    public class Matrix2D
    {
        public double M11;
        public double M12;
        public double M13;
        public double M21;
        public double M22;
        public double M23;
        public Matrix2D()
        { }
        public Matrix2D(BinaryReverseReader r)
        {
            this.M11 = r.ReadDouble();
            this.M12 = r.ReadDouble();
            this.M13 = r.ReadDouble();
            this.M21 = r.ReadDouble();
            this.M22 = r.ReadDouble();
            this.M23 = r.ReadDouble();
        }
    }

    [Description("TySh")]
    public class TypeToolObject : PhotoshopFile.Layer.AdjustmentLayerInfo
    {


        public Matrix2D Transform;
        public DynVal TxtDescriptor;
        [XmlIgnoreAttribute()]
        public DynVal WarpDescriptor;
        [XmlIgnoreAttribute()]
        public RectangleF WarpRect;
        public TdTaStylesheetReader StylesheetReader;
        public Dictionary<string, object> engineData;
        public Boolean isTextHorizontal
        {
            get
            {
                return ((string)TxtDescriptor.Children.Find(c => c.Name.Equals("Orientation", StringComparison.InvariantCultureIgnoreCase)).Value)
                     .Equals("Orientation.Horizontal", StringComparison.InvariantCultureIgnoreCase);
            }
        }


        public TypeToolObject(PhotoshopFile.Layer.AdjustmentLayerInfo info)
        {
            this.m_data = info.Data;
            this.m_key = info.Key;
            this.m_layer = info.Layer;
            
            BinaryReverseReader r = this.DataReader;

            ushort PhotshopVersion = r.ReadUInt16(); //2 bytes, =1 Photoshop 6 (not 5)

            this.Transform = new Matrix2D(r); //six doubles (48 bytes)

            ushort TextVersion = r.ReadUInt16(); //2 bytes, =50. For Photoshop 6.0.
            uint DescriptorVersion = r.ReadUInt32(); //4 bytes,=16. For Photoshop 6.0.


            this.TxtDescriptor = DynVal.ReadDescriptor(r); //Text descriptor

            ushort WarpVersion = r.ReadUInt16(); //2 bytes, =1. For Photoshop 6.0.
            uint WarpDescriptorVersion = r.ReadUInt32(); //4 bytes, =16. For Photoshop 6.0.

            engineData = (Dictionary<string, object>)TxtDescriptor.Children.Find(c => c.Name == "EngineData").Value;
            StylesheetReader = new TdTaStylesheetReader(engineData);

            //string desc = this.TxtDescriptor.getString();

            this.WarpDescriptor = DynVal.ReadDescriptor(r); //Warp descriptor
            this.Data = r.ReadBytes((int)r.BytesToEnd); //17 bytes???? All zeroes?
            if (Data.Length != 17 || !(Array.TrueForAll(Data, b => b == 0)))
            {
                string s = ReadableBinary.CreateHexEditorString(Data);
                Debug.Write(s);
            }
           
            //this.WarpRect = new RectangleF();
            //WarpRect.X = (float)r.ReadDouble();
            //WarpRect.Y = (float)r.ReadDouble();
            //this.Data.
            //WarpRect.Width = (float)r.ReadDouble() - WarpRect.X;
            //WarpRect.Height = (float)r.ReadDouble() - WarpRect.Y;

            //this.Data = r.ReadBytes((int)r.BytesToEnd);
        }

       

    }
}
