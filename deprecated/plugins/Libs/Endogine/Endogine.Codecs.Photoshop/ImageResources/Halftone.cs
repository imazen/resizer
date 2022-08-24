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
    public class ColorHalftoneInfo : ImageResource
    {
        public class Screen
        {
            public enum Shape
            {
                Round, Ellipse, Line, Square, Cross, Diamond, Custom = 99
            }

            [XmlAttributeAttribute()]
            public float FrequenceValue;
            [XmlAttributeAttribute()]
            public ushort FrequenceScale;
            [XmlAttributeAttribute()]
            public float Angle;
            [XmlAttributeAttribute()]
            public short ShapeCode;
            [XmlAttributeAttribute()]
            public bool AccurateScreens;
            [XmlAttributeAttribute()]
            public bool DefaultScreens;

            public Screen()
            { }
            public Screen(BinaryPSDReader reader)
            {
                this.FrequenceValue = reader.ReadSingle(); //TODO: fixed single
                this.FrequenceScale = reader.ReadUInt16();
                this.Angle = reader.ReadSingle(); //TODO: fixed single
                this.ShapeCode = reader.ReadInt16();
                reader.BaseStream.Position += 4;
                this.AccurateScreens = reader.ReadBoolean();
                this.DefaultScreens = reader.ReadBoolean();
            }

        }

        public List<Screen> Screens;
        public List<string> Curves; //TODO:!!

        public ColorHalftoneInfo()
        { }

        public ColorHalftoneInfo(ImageResource imgRes)
            : base(imgRes)
        {
            BinaryPSDReader reader = imgRes.GetDataReader();

            this.Screens = new List<Screen>();
            for (int i = 0; i < 4; i++)
            {
                this.Screens.Add(new Screen(reader));
            }

            reader.Close();
        }

        protected override void SubWrite(BinaryPSDWriter writer)
        {
        }
    }
}
