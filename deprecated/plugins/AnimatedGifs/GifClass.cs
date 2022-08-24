/* Copyright (c) 2014 Imazen See license.txt for your rights */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
namespace ImageResizer.Plugins.AnimatedGifs
{
    /// <summary>
    /// Dissects a GIF image into its component parts.
    /// </summary>
    public class GifClass
    {
        public GIFVersion m_Version = GIFVersion.GIF87a;
        
        public List<byte> m_GifSignature = new List<byte>();
        public List<byte> m_ScreenDescriptor = new List<byte>();
        public List<byte> m_ColorTable = new List<byte>();
        public List<byte> m_ImageDescriptor = new List<byte>();
        public List<byte> m_ImageData = new List<byte>();
        public GifClass()
        { }
        public void LoadGifPicture(string filename)
        { LoadGifPicture((Bitmap)Bitmap.FromFile(filename)); }

        public void LoadGifPicture(Bitmap gifPicture)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                gifPicture.Save(stream, ImageFormat.Gif);
                LoadGifPicture(stream);
            }
        }
        public void LoadGifPicture(MemoryStream stream)
        {
           //TODO: Eliminate double copying
            List<byte> dataList = new List<byte>(stream.ToArray()); //Copies the data *again* grr.
            
            if (!AnalyzeGifSignature(dataList))
            {
                throw (new Exception("File is not a GIF!"));
            }
            AnalyzeScreenDescriptor(dataList);
            GIFBlockType blockType = GetTypeOfNextBlock(dataList);
            while (blockType != GIFBlockType.Trailer)
            {
                switch (blockType)
                {
                    case GIFBlockType.ImageDescriptor:
                        AnalyzeImageDescriptor(dataList);
                        break;
                    case GIFBlockType.Extension:
                        ThrowAwayExtensionBlock(dataList);
                        break;
                    default:
                        break;
                }
                blockType = GetTypeOfNextBlock(dataList);
            }
        }
        private bool AnalyzeGifSignature(List<byte> gifData)
        {
            for (int i = 0; i < 6; i++)
                m_GifSignature.Add(gifData[i]);
            
            gifData.RemoveRange(0, 6);
            List<char> chars = m_GifSignature.ConvertAll<char>(new Converter<byte, char>(ByteToChar));
            string s = new string(chars.ToArray());
            if (s == GIFVersion.GIF89a.ToString())
                m_Version = GIFVersion.GIF89a;
            else if (s == GIFVersion.GIF87a.ToString())
                m_Version = GIFVersion.GIF87a;
            else
                return false;
            return true;
        }
        private char ByteToChar(byte b)
        { return (char)b; }
        private void AnalyzeScreenDescriptor(List<byte> gifData)
        {
            for (int i = 0; i < 7; i++)
                m_ScreenDescriptor.Add(gifData[i]);
            gifData.RemoveRange(0, 7);
            // if the first bit of the fifth byte is set the GlobalColorTable follows this block
            bool globalColorTableFollows = (m_ScreenDescriptor[4] & 0x80) != 0;
            if (globalColorTableFollows)
            {
                int pixel = m_ScreenDescriptor[4] & 0x07;
                int lengthOfColorTableInByte = 3 * ((int)Math.Pow(2, pixel + 1));
                for (int i = 0; i < lengthOfColorTableInByte; i++)
                    m_ColorTable.Add(gifData[i]);
                gifData.RemoveRange(0, lengthOfColorTableInByte);
            }
            m_ScreenDescriptor[4] = (byte)(m_ScreenDescriptor[4] & 0x7F);
        }
        private GIFBlockType GetTypeOfNextBlock(List<byte> gifData)
        {
            GIFBlockType blockType = (GIFBlockType)gifData[0];
            return blockType;
        }
        private void AnalyzeImageDescriptor(List<byte> gifData)
        {
            for (int i = 0; i < 10; i++)
                m_ImageDescriptor.Add(gifData[i]);
            gifData.RemoveRange(0, 10);
            // get ColorTable if exists
            bool localColorMapFollows = (m_ImageDescriptor[9] & 0x80) != 0;
            if (localColorMapFollows)
            {
                int pixel = m_ImageDescriptor[9] & 0x07;
                int lengthOfColorTableInByte = 3 * ((int)Math.Pow(2, pixel + 1));
                m_ColorTable.Clear();
                for (int i = 0; i < lengthOfColorTableInByte; i++)
                    m_ColorTable.Add(gifData[i]);
                gifData.RemoveRange(0, lengthOfColorTableInByte);
            }
            else
            {
                int lastThreeBitsOfGlobalTableDescription = m_ScreenDescriptor[4] & 0x07;
                m_ImageDescriptor[9] = (byte)(m_ImageDescriptor[9] & 0xF8);
                m_ImageDescriptor[9] = (byte)(m_ImageDescriptor[9] | lastThreeBitsOfGlobalTableDescription);
            }
            m_ImageDescriptor[9] = (byte)(m_ImageDescriptor[9] | 0x80);
            GetImageData(gifData);
        }
        private void GetImageData(List<byte> gifData)
        {
            m_ImageData.Add(gifData[0]);
            gifData.RemoveAt(0);
            while (gifData[0] != 0x00)
            {
                int countOfFollowingDataBytes = gifData[0];
                for (int i = 0; i <= countOfFollowingDataBytes; i++)
                {
                    m_ImageData.Add(gifData[i]);
                }
                gifData.RemoveRange(0, countOfFollowingDataBytes + 1);
            }
            m_ImageData.Add(gifData[0]);
            gifData.RemoveAt(0);
        }
        private void ThrowAwayExtensionBlock(List<byte> gifData)
        {
            gifData.RemoveRange(0, 2); // Delete ExtensionBlockIndicator and ExtensionDetermination
            while (gifData[0] != 0)
            {
                gifData.RemoveRange(0, gifData[0] + 1);
            }
            gifData.RemoveAt(0);
        }
    }
}