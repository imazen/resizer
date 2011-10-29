using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.Test.Tools.WicCop.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Reflection;

namespace ImageResizer.Plugins.Wic {
    public class ConversionUtils {


        private static object _lockBppDict = new object();
        private static Dictionary<Guid,int> _bppDict = null;

        public static int Bpp(Guid format){
            if (_bppDict == null) lock(_lockBppDict) if (_bppDict == null){
                FieldInfo[] members = typeof(Consts).GetFields( System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.GetField);
                Dictionary<Guid,int> d =new Dictionary<Guid,int>();
                foreach(FieldInfo fi in members){
                    if (fi.FieldType != typeof(Guid)) continue;
                    Guid key = (Guid)fi.GetValue(null);

                    string name = fi.Name;
                    

                    d[key] = name;
                }
                _bppDict = d;
            }
            int temp;
            if (!_bppDict.TryGetValue(format, out temp)) throw new Exception("No idea what format this is " + format.ToString()); 
            return temp;
        }

        public static Bitmap FromWic(IWICBitmapSource source) {
            return null;
            var factory = (IWICComponentFactory)new WICImagingFactory();
            //var converter = factory.CreateFormatConverter();
            //TODO: handle conversion. converter.Initialize(b, GUID_WICPixelFormat24bppBGR, WICBitmapDitherType.

            uint w, h;
            source.GetSize(out w, out h);
            Guid format;
            source.GetPixelFormat(out format);

            //    uint dword = 4;
            //    uint stride = ;

            //    UINT cbStride = uWidth * 3;
            //    // Force the stride to be a multiple of sizeof(DWORD)
            //    cbStride = ((cbStride + sizeof(DWORD) - 1) / sizeof(DWORD)) * sizeof(DWORD);
            //    stride = ((stride + dword - 1)/ dword) * dword; //Round up

            //    uint bufferSize = stride * h;
            //    byte[] buffer = new byte[bufferSize];

            //    Marshal.AllocHGlobal(
            //    source.CopyPixels(
            //    MarshalByRefObject.
            //    Bitmap bmp = new Bitmap(w,h,stride, System.Drawing.Imaging.PixelFormat.Format24bppRgb,


            //    Consts.
            //    HRESULT hr = S_OK;
            //UINT uWidth = 0;
            //UINT uHeight = 0;
            //WICPixelFormatGUID pixelFormat = GUID_NULL;
            //IWICImagingFactory *piImagingFactory = NULL;
            //IWICFormatConverter *piFormatConverter = NULL;
            //Bitmap *pGdiPlusBitmap = NULL;
            //BYTE *pbBuffer = NULL;

            //if (!piBitmapSource || !ppGdiPlusBitmap)
            //    return ERROR_INVALID_PARAMETER;

            //IFS(CoCreateInstance(CLSID_WICImagingFactory, NULL, CLSCTX_INPROC_SERVER, IID_IWICImagingFactory, (LPVOID*) &piImagingFactory));
            //IFS(piImagingFactory->CreateFormatConverter(&piFormatConverter));
            //IFS(piFormatConverter->Initialize(piBitmapSource, GUID_WICPixelFormat24bppBGR, WICBitmapDitherTypeNone, NULL, 0.0, WICBitmapPaletteTypeCustom));
            //IFS(piFormatConverter->GetSize(&uWidth, &uHeight));
            //IFS(piFormatConverter->GetPixelFormat(&pixelFormat));

            //if (SUCCEEDED(hr))
            //{
            //    UINT cbStride = uWidth * 3;
            //    // Force the stride to be a multiple of sizeof(DWORD)
            //    cbStride = ((cbStride + sizeof(DWORD) - 1) / sizeof(DWORD)) * sizeof(DWORD);

            //    UINT cbBufferSize = cbStride * uHeight;
            //    pbBuffer = new BYTE[cbBufferSize];

            //    if (pbBuffer != NULL)
            //    {
            //        WICRect rc = { 0, 0, uWidth, uHeight };
            //        IFS(piFormatConverter->CopyPixels(&rc, cbStride, cbStride * uHeight, pbBuffer));
            //        pGdiPlusBitmap = new Bitmap(uWidth, uHeight, cbStride, PixelFormat24bppRGB , pbBuffer);
            //    }
            //    else
            //    {
            //        hr = ERROR_NOT_ENOUGH_MEMORY;
            //    }
            //}

            //*ppGdiPlusBitmap = pGdiPlusBitmap;
            //RELEASE_INTERFACE(piFormatConverter);
            //RELEASE_INTERFACE(piImagingFactory);
            //if (ppbGdiPlusBuffer)
            //    *ppbGdiPlusBuffer = pbBuffer;
            //return hr;
        }
    }
}
