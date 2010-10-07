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

namespace Endogine.Codecs.Photoshop.ImageResources
{
	/// <summary>
	/// Summary description for ResolutionInfo.
	/// </summary>
	public class ResolutionInfo : ImageResource
	{
		public short hRes = 72;
		public int hResUnit = 1;
		public short widthUnit = 2;

		public short vRes = 72;
		public int vResUnit = 1;
		public short heightUnit = 2;

        public ResolutionInfo()
        { }

		public ResolutionInfo(ImageResource imgRes) : base(imgRes)
		{
			//m_bResolutionInfoFilled = true;
			BinaryPSDReader reader = imgRes.GetDataReader();

			this.hRes = reader.ReadInt16();
			this.hResUnit = reader.ReadInt32();
			this.widthUnit = reader.ReadInt16();

			this.vRes = reader.ReadInt16();
			this.vResUnit = reader.ReadInt32();
			this.heightUnit = reader.ReadInt16();

			reader.Close();
		}

        protected override void SubWrite(BinaryPSDWriter writer)
        {
            writer.Write(this.hRes);
            writer.Write(this.hResUnit);
            writer.Write(this.widthUnit);

            writer.Write(this.vRes);
            writer.Write(this.vResUnit);
            writer.Write(this.heightUnit);
        }
	}
}
