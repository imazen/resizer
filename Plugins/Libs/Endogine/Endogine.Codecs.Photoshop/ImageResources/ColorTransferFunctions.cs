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
using System.Text;
using System.Xml.Serialization;

namespace Endogine.Codecs.Photoshop.ImageResources
{
	/// <summary>
	/// Summary description for CopyRightInfo.
	/// </summary>
	public class ColorTransferFunctions : ImageResource
	{
        public class ColorTransferFunction
        {
            [XmlIgnoreAttribute]
            public List<short> Curve;
            [XmlAttributeAttribute]
            public string CurveForXml
            {
                get
                {
                    string s = "";
                    foreach (short val in this.Curve)
                        s += val + ";";
                    return s.Remove(s.Length - 1);
                }
                set { }
            }
            [XmlAttributeAttribute]
            public bool Override;

            public ColorTransferFunction()
            { }
            public ColorTransferFunction(BinaryPSDReader reader)
            {
                this.Curve = new List<short>();
                for (int i = 0; i < 13; i++)
                {
                    this.Curve.Add(reader.ReadInt16());
                }
                if (this.Curve[0] == -1 || this.Curve[12] == -1)
                    throw new Exception("Error");
                this.Override = reader.ReadUInt16() > 0 ? true : false;
            }
        }

        public List<ColorTransferFunction> Functions;
        public ColorTransferFunctions()
        { }

        public ColorTransferFunctions(ImageResource imgRes)
            : base(imgRes)
		{
			BinaryPSDReader reader = imgRes.GetDataReader();
            this.Functions = new List<ColorTransferFunction>();
            for (int i = 0; i < 4; i++)
                this.Functions.Add(new ColorTransferFunction(reader));
            reader.Close();
		}

        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
	}
}
