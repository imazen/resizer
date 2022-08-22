/* Copyright (c) 2014 Imazen See license.txt for your rights */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace PhotoshopFile
{

    public class ReadableBinary
    {
        // Methods
        public static string CreateHexEditorString(byte[] data)
        {
            return CreateHexEditorString(data, 0, data.Length);
        }

        public static string CreateHexEditorString(byte[] data, int start, int length)
        {
            int pos = 0;
            StringBuilder sbAll = new StringBuilder("\r\n0000:");
            StringBuilder sbChars = new StringBuilder();
            for (int i = start; i < (start + length); i++)
            {
                byte num3 = data[i];
                if (((((num3 >= 0x20) && (num3 <= 0x7e)) || (num3 >= 160)) && ((num3 != 60) && (num3 != 0x3e))) && (num3 != 0x26))
                {
                    sbChars.Append((char)num3);
                }
                else
                {
                    sbChars.Append(".");
                }
                sbAll.Append(num3.ToString("X",NumberFormatInfo.InvariantInfo).PadLeft(2, '0') + " ");
                pos++;
                if ((pos % 8) == 0)
                {
                    sbAll.Append("  ");
                    sbChars.Append("  ");
                }
                if (((pos % 0x10) == 0) || (pos == 0))
                {
                    NewLine(sbAll, sbChars, pos);
                    sbChars = new StringBuilder();
                }
            }
            if (sbChars.Length > 0)
            {
                NewLine(sbAll, sbChars, pos);
            }
            string str = sbAll.ToString();
            return str.Remove(str.LastIndexOf("\r"));
        }

        private static void NewLine(StringBuilder sbAll, StringBuilder sbChars, int pos)
        {
            sbAll.Append("  ");
            sbAll.Append(sbChars);
            sbAll.Append("\r\n" + pos.ToString("X", NumberFormatInfo.InvariantInfo).PadLeft(4, '0') + ":");
        }
    }




}
