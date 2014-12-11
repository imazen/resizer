//----------------------------------------------------------------------------------------
// THIS CODE AND INFORMATION IS PROVIDED "AS-IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//----------------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Microsoft.Test.Tools.WicCop.InteropServices.ComTypes
{
    public enum WICMetadataCreationOptions : uint
    {
        WICMetadataCreationDefault = 0x00000000,
        WICMetadataCreationAllowUnknown = WICMetadataCreationDefault,
        WICMetadataCreationFailUnknown = 0x00010000,
        WICMetadataCreationMask = 0xFFFF0000
    };

    public enum WICPersistOptions : uint
    {
        WICPersistOptionDefault = 0x00000000,
        WICPersistOptionLittleEndian = 0x00000000,
        WICPersistOptionBigEndian = 0x00000001,
        WICPersistOptionStrictFormat = 0x00000002,
        WICPersistOptionNoCacheStream = 0x00000004,
        WICPersistOptionPreferUTF8 = 0x00000008,
        WICPersistOptionMask = 0x0000FFFF
    };

    [Guid("FEAA2A8D-B3F3-43E4-B25C-D1DE990A1AE1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IWICMetadataBlockReader
    {
        void GetContainerFormat(
            out Guid pguidContainerFormat
            );

        uint GetCount();

        IWICMetadataReader GetReaderByIndex(
            uint nIndex
            );

        IEnumUnknown GetEnumerator();
    }

    [Guid("08FB9676-B444-41E8-8DBE-6A53A542BFF1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IWICMetadataBlockWriter : IWICMetadataBlockReader
    {
        #region IWICMetadataBlockReader
        new void GetContainerFormat(
            out Guid pguidContainerFormat
            );

        new uint GetCount();

        new IWICMetadataReader GetReaderByIndex(
            uint nIndex
            );

        new IEnumUnknown GetEnumerator();
        #endregion

        void InitializeFromBlockReader(
            IWICMetadataBlockReader pIMDBlockReader
            );

        IWICMetadataWriter GetWriterByIndex(
            uint nIndex
            );

        void AddWriter(
            IWICMetadataWriter pIMetadataWriter
            );

        void SetWriterByIndex(
            uint nIndex, 
            IWICMetadataWriter pIMetadataWriter
            );

        void RemoveWriterByIndex(
            uint nIndex
            );
    }

    [Guid("9204FE99-D8FC-4FD5-A001-9536B067A899")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IWICMetadataReader
    {
        void GetMetadataFormat(
            out Guid pguidMetadataFormat
            );

        IWICMetadataHandlerInfo GetMetadataHandlerInfo();

        uint GetCount();

        void GetValueByIndex(
            uint nIndex,
            [In]
            [Out]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarSchema,
            [In]
            [Out]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarId,
            [In]
            [Out]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarValue
            );

        void GetValue(
            [In]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarSchema,
            [In]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarId,
            [In]
            [Out]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarValue
            );

        IWICEnumMetadataItem GetEnumerator();
    }

    [Guid("F7836E16-3BE0-470B-86BB-160D0AECD7DE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IWICMetadataWriter : IWICMetadataReader
    {
        #region IWICMetadataReader
        new void GetMetadataFormat(
            out Guid pguidMetadataFormat
            );

        new IWICMetadataHandlerInfo GetMetadataHandlerInfo();

        new uint GetCount();

        new void GetValueByIndex(uint nIndex,
            [In]
            [Out]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarSchema,
            [In]
            [Out]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarId,
            [In]
            [Out]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarValue
            );

        new void GetValue(
            [In]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarSchema,
            [In]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarId,
            [In]
            [Out]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarValue
            );

        new IWICEnumMetadataItem GetEnumerator();
        #endregion

        void SetValue(
            [In]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarSchema,
            [In]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarId,
            [In]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarValue
            );

        void SetValueByIndex(
            uint nIndex,
            [In]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarSchema,
            [In]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarId,
            [In]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarValue
            );

        void RemoveValue(
            [In]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarSchema,
            [In]
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(PropVariantMarshaler))]
            PropVariant pvarId
            );

        void RemoveValueByIndex(
            uint nIndex
            );
    }

    [Guid("449494BC-B468-4927-96D7-BA90D31AB505")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IWICStreamProvider
    {
        IStream GetStream();

        uint GetPersistOptions();

        void GetPreferredVendorGUID(
            out Guid pguidPreferredVendor
            );

        void RefreshStream();
    }

    [Guid("ABA958BF-C672-44D1-8D61-CE6DF2E682C2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IWICMetadataHandlerInfo : IWICComponentInfo
    {
        #region IWICComponentInfo
        new WICComponentType GetComponentType();

        new void GetCLSID(
            out Guid pclsid
            );

        new WICComponentSigning GetSigningStatus();

        new uint GetAuthor(
            uint cchAuthor,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] 
            StringBuilder wzAuthor
            );

        new void GetVendorGUID(
            out Guid pguidVendor
            );

        new uint GetVersion(
            uint cchVersion,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] 
            StringBuilder wzVersion
            );

        new uint GetSpecVersion(
            uint cchSpecVersion,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] 
            StringBuilder wzSpecVersion
            );

        new uint GetFriendlyName(
            uint cchFriendlyName,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] 
            StringBuilder wzFriendlyName
            );
        #endregion

        void GetMetadataFormat(
            out Guid pguidMetadataFormat
            );

        uint GetContainerFormats(
            uint cContainerFormats,
            [Out]
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] 
            Guid[] pguidContainerFormats
            );

        uint GetDeviceManufacturer(
            uint cchDeviceManufacturer,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] 
            StringBuilder wzDeviceManufacturer
            );

        uint GetDeviceModels(
            uint cchDeviceModels,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)]  
            StringBuilder wzDeviceModels
            );

        [return: MarshalAs(UnmanagedType.Bool)]
        bool DoesRequireFullStream();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool DoesSupportPadding();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool DoesRequireFixedSize();
    }

    public struct WICMetadataPattern
    {
        public ulong Position;
        public uint Length;
        public IntPtr Pattern;
        public IntPtr Mask;
        public ulong DataOffset;
    };

    [Guid("EEBF1F5B-07C1-4447-A3AB-22ACAF78A804")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IWICMetadataReaderInfo : IWICMetadataHandlerInfo
    {
        #region IWICMetadataHandlerInfo
        #region IWICComponentInfo
        new WICComponentType GetComponentType();

        new void GetCLSID(
            out Guid pclsid
            );

        new WICComponentSigning GetSigningStatus();

        new uint GetAuthor(
            uint cchAuthor,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] 
            StringBuilder wzAuthor
            );

        new void GetVendorGUID(
            out Guid pguidVendor
            );

        new uint GetVersion(
            uint cchVersion,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] 
            StringBuilder wzVersion
            );

        new uint GetSpecVersion(
            uint cchSpecVersion,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] 
            StringBuilder wzSpecVersion
            );

        new uint GetFriendlyName(
            uint cchFriendlyName,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] 
            StringBuilder wzFriendlyName
            );
        #endregion

        new void GetMetadataFormat(
            out Guid pguidMetadataFormat
            );

        new uint GetContainerFormats(
            uint cContainerFormats,
            [Out]
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
            Guid[] pguidContainerFormats
            );

        new uint GetDeviceManufacturer(
            uint cchDeviceManufacturer,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)]
            StringBuilder wzDeviceManufacturer
            );

        new uint GetDeviceModels(
            uint cchDeviceModels,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)]
            StringBuilder wzDeviceModels
            );

        [return: MarshalAs(UnmanagedType.Bool)]
        new bool DoesRequireFullStream();

        [return: MarshalAs(UnmanagedType.Bool)]
        new bool DoesSupportPadding();

        [return: MarshalAs(UnmanagedType.Bool)]
        new bool DoesRequireFixedSize();
        #endregion

        uint GetPatterns(
            [MarshalAs(UnmanagedType.LPStruct)]
            Guid guidContainerFormat,
            uint cbSize,
            IntPtr pPattern,
            out uint pcCount
            );

        [return: MarshalAs(UnmanagedType.Bool)]
        bool MatchesPattern(
            [MarshalAs(UnmanagedType.LPStruct)] 
            Guid guidContainerFormat,
            IStream pIStream
            );

        IWICMetadataReader CreateInstance();
    }

    public struct WICMetadataHeader
    {
        public ulong Position;
        public uint Length;
        public IntPtr Header;
        public ulong DataOffset;
    };

    [Guid("B22E3FBA-3925-4323-B5C1-9EBFC430F236")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IWICMetadataWriterInfo : IWICMetadataHandlerInfo
    {
        #region IWICMetadataHandlerInfo
        #region IWICComponentInfo
        new WICComponentType GetComponentType();

        new void GetCLSID(
            out Guid pclsid
            );

        new WICComponentSigning GetSigningStatus();

        new uint GetAuthor(
            uint cchAuthor,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] 
            StringBuilder wzAuthor
            );

        new void GetVendorGUID(out Guid pguidVendor);

        new uint GetVersion(
            uint cchVersion,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] 
            StringBuilder wzVersion
            );

        new uint GetSpecVersion(
            uint cchSpecVersion,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] 
            StringBuilder wzSpecVersion
            );

        new uint GetFriendlyName(
            uint cchFriendlyName,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)] 
            StringBuilder wzFriendlyName
            );
        #endregion

        new void GetMetadataFormat(
            out Guid pguidMetadataFormat
            );

        new uint GetContainerFormats(
            uint cContainerFormats,
            [Out]
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
            Guid[] pguidContainerFormats
            );

        new uint GetDeviceManufacturer(
            uint cchDeviceManufacturer,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)]
            StringBuilder wzDeviceManufacturer
            );

        new uint GetDeviceModels(
            uint cchDeviceModels,
            [MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 1)]
            StringBuilder wzDeviceModels
            );

        [return: MarshalAs(UnmanagedType.Bool)]
        new bool DoesRequireFullStream();

        [return: MarshalAs(UnmanagedType.Bool)]
        new bool DoesSupportPadding();

        [return: MarshalAs(UnmanagedType.Bool)]
        new bool DoesRequireFixedSize();
        #endregion

        uint GetHeader(
            [MarshalAs(UnmanagedType.LPStruct)] 
            Guid guidContainerFormat,
            uint cbSize,
            IntPtr pHeader
            );

        IWICMetadataWriter CreateInstance();
    }

    [Guid("412D0C3A-9650-44FA-AF5B-DD2A06C8E8FB")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IWICComponentFactory : IWICImagingFactory
    {
        #region IWICImagingFactory
        new IWICBitmapDecoder CreateDecoderFromFilename(
            [MarshalAs(UnmanagedType.LPWStr)]
            string wzFilename,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]
            Guid[] pguidVendor,
            NativeMethods.GenericAccessRights dwDesiredAccess,
            WICDecodeOptions metadataOptions
        );

        new IWICBitmapDecoder CreateDecoderFromStream(
            IStream pIStream,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]
            Guid[] pguidVendor,
            WICDecodeOptions metadataOptions
        );

        new IWICBitmapDecoder CreateDecoderFromFileHandle(
            IntPtr hFile,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]
            Guid[] pguidVendor,
            WICDecodeOptions metadataOptions
            );

        new IWICComponentInfo CreateComponentInfo(
            [MarshalAs(UnmanagedType.LPStruct)]
            Guid clsidComponent
            );

        new IWICBitmapDecoder CreateDecoder(
            [MarshalAs(UnmanagedType.LPStruct)]
            Guid guidContainerFormat,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]
            Guid[] pguidVendor
            );

        new IWICBitmapEncoder CreateEncoder(
            [MarshalAs(UnmanagedType.LPStruct)]
            Guid guidContainerFormat,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]
            Guid[] pguidVendor
            );

        new IWICPalette CreatePalette();

        new IWICFormatConverter CreateFormatConverter();

        new IWICBitmapScaler CreateBitmapScaler();

        new IWICBitmapClipper CreateBitmapClipper();

        new IWICBitmapFlipRotator CreateBitmapFlipRotator();

        new IWICStream CreateStream();

        new IWICColorContext CreateColorContext();

        new IWICColorTransform CreateColorTransform();

        new IWICBitmap CreateBitmap(
            uint uiWidth,
            uint uiHeight,
            [MarshalAs(UnmanagedType.LPStruct)]
            Guid pixelFormat,
            WICBitmapCreateCacheOption option
            );

        new IWICBitmap CreateBitmapFromSource(
            IWICBitmapSource pIBitmapSource,
            WICBitmapCreateCacheOption option
            );

        new IWICBitmap CreateBitmapFromSourceRect(
            IWICBitmapSource pIBitmapSource,
            uint x,
            uint y,
            uint width,
            uint height
            );

        new IWICBitmap CreateBitmapFromMemory(
            uint uiWidth,
            uint uiHeight,
            [MarshalAs(UnmanagedType.LPStruct)] 
            Guid pixelFormat,
            uint cbStride,
            uint cbBufferSize,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] 
            byte[] pbBuffer
            );

        new IWICBitmap CreateBitmapFromHBITMAP(
            IntPtr hBitmap,
            IntPtr hPalette,
            WICBitmapAlphaChannelOption options
            );

        new IWICBitmap CreateBitmapFromHICON(
            IntPtr hIcon
            );

        new IEnumUnknown CreateComponentEnumerator(
            WICComponentType componentTypes,           /* WICComponentType */
            WICComponentEnumerateOptions options       /* WICComponentEnumerateOptions */
            );

        new IWICFastMetadataEncoder CreateFastMetadataEncoderFromDecoder(
            IWICBitmapDecoder pIDecoder
            );

        new IWICFastMetadataEncoder CreateFastMetadataEncoderFromFrameDecode(
            IWICBitmapFrameDecode pIFrameDecoder
            );

        new IWICMetadataQueryWriter CreateQueryWriter(
            [MarshalAs(UnmanagedType.LPStruct)]
            Guid guidMetadataFormat,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]
            Guid[] pguidVendor
            );

        new IWICMetadataQueryWriter CreateQueryWriterFromReader(
            IWICMetadataQueryReader pIQueryReader,
            [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)]
            Guid[] pguidVendor
            );
        #endregion

        IWICMetadataReader CreateMetadataReader(
            [In]
            [MarshalAs(UnmanagedType.LPStruct)] 
            Guid guidMetadataFormat,
            [In] 
            ref Guid pguidVendor,
            uint dwOptions,
            IStream pIStream
            );

        IWICMetadataReader CreateMetadataReaderFromContainer(
            [In]
            [MarshalAs(UnmanagedType.LPStruct)]
            Guid guidContainerFormat,
            [In]
            ref Guid pguidVendor,
            uint dwOptions,
            IStream pIStream
            );

        IWICMetadataWriter CreateMetadataWriter(
            [In]
            [MarshalAs(UnmanagedType.LPStruct)] 
            Guid guidMetadataFormat,
            [In] 
            ref Guid pguidVendor,
            uint dwMetadataOptions
            );

        IWICMetadataWriter CreateMetadataWriterFromReader(
            [In]
            IWICMetadataReader pIReader,
            [In] 
            ref Guid pguidVendor
            );

        IWICMetadataQueryReader CreateQueryReaderFromBlockReader(
            [In] 
            IWICMetadataBlockReader pIBlockReader
            );

        IWICMetadataQueryWriter CreateQueryWriterFromBlockWriter(
            [In] 
            IWICMetadataBlockWriter pIBlockWriter
            );

        IPropertyBag2 CreateEncoderPropertyBag(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] 
            PROPBAG2[] ppropOptions,
            uint cCount
            );
    }
}