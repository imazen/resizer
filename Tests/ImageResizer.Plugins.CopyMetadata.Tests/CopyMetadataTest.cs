using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Text;
using Gallio.Common.Reflection;
using Gallio.Framework;
using ImageResizer;
using ImageResizer.Plugins;
using ImageResizer.Plugins.CopyMetadata;
using ImageResizer.Resizing;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;

namespace ImageResizer.Plugins.CopyMetadata.Tests
{
    [TestFixture]
    public class CopyMetadataTest
    {
        [Test(Order = 1)]
        public void SupportsCopyMetadataQuerystring()
        {
            CopyMetadataPlugin plugin = new CopyMetadataPlugin();

            plugin.GetSupportedQuerystringKeys().Contains("copymetadata");
        }

        [Test(Order = 2)]
        public void ShouldCopyMetadataWhenRequested()
        {
            CopyMetadataPlugin plugin = new CopyMetadataPlugin();

            using (ImageState state = CreateImageState(true))
            {
                RequestedAction requestedAction = CallProcessFinalBitmap(plugin, state);
                Assert.AreEqual(RequestedAction.None, requestedAction);

                Assert.IsNotEmpty(state.destBitmap.PropertyIdList);
                Assert.IsNotEmpty(state.destBitmap.PropertyItems);

                // Ensure that all the properties came from the original image...
                foreach (PropertyItem prop in state.destBitmap.PropertyItems)
                {
                    PropertyItem sourceProp = state.sourceBitmap.PropertyItems.SingleOrDefault(p => p.Id == prop.Id);
                    Assert.IsNotNull(sourceProp, "destBitmap ended up with a property that sourceBitmap didn't have!");
                   
                    Assert.AreEqual(sourceProp.Len, prop.Len);
                    Assert.AreEqual(sourceProp.Type, prop.Type);
                    Assert.AreEqual(sourceProp.Len, prop.Len);
                    Assert.AreElementsEqual(sourceProp.Value, prop.Value);
                }
            }
        }

        [Test(Order = 3)]
        public void ShouldNotCopyMetadataWhenNotRequested()
        {
            CopyMetadataPlugin plugin = new CopyMetadataPlugin();

            using (ImageState state = CreateImageState(false))
            {
                RequestedAction requestedAction = CallProcessFinalBitmap(plugin, state);
                Assert.AreEqual(RequestedAction.None, requestedAction);

                Assert.IsEmpty(state.destBitmap.PropertyIdList);
                Assert.IsEmpty(state.destBitmap.PropertyItems);
            }
        }

        [Test(Order = 4)]
        public void ShouldNeverCopyExcludedProperties()
        {
            CopyMetadataPlugin plugin = new CopyMetadataPlugin();

            using (ImageState state = CreateImageState(true))
            {
                RequestedAction requestedAction = CallProcessFinalBitmap(plugin, state);
                Assert.AreEqual(RequestedAction.None, requestedAction);

                // Make sure some of properties were stripped...
                
                // PropertyTagOrientation
                Assert.Exists(state.sourceBitmap.PropertyItems, prop => prop.Id == 0x0112);
                Assert.DoesNotExist(state.destBitmap.PropertyItems, prop => prop.Id == 0x0112);

                // PropertyTagXResolution
                Assert.Exists(state.sourceBitmap.PropertyItems, prop => prop.Id == 0x011A);
                Assert.DoesNotExist(state.destBitmap.PropertyItems, prop => prop.Id == 0x011A);

                // PropertyTagYResolution
                Assert.Exists(state.sourceBitmap.PropertyItems, prop => prop.Id == 0x011B);
                Assert.DoesNotExist(state.destBitmap.PropertyItems, prop => prop.Id == 0x011B);
            }
        }

        [Test(Order = 5)]
        public void CopiedMetadataIncludesGeolocationInformation()
        {
            CopyMetadataPlugin plugin = new CopyMetadataPlugin();

            using (ImageState state = CreateImageState(true))
            {
                RequestedAction requestedAction = CallProcessFinalBitmap(plugin, state);
                Assert.AreEqual(RequestedAction.None, requestedAction);

                // Make sure geolocation properties got copied...
                int[] geolocationProperties = new int[] {
                    0x0000, // PropertyTagGpsVer
                    0x0001, // PropertyTagGpsLatitudeRef
                    0x0002, // PropertyTagGpsLatitude
                    0x0003, // PropertyTagGpsLongitudeRef
                    0x0004, // PropertyTagGpsLongitude
                    0x0005, // PropertyTagGpsAltitudeRef
                    0x0006, // PropertyTagGpsAltitude
                };

                foreach (int propId in geolocationProperties)
                {
                    Assert.Exists(state.sourceBitmap.PropertyItems, prop => prop.Id == propId, "sourceBitmap did not include geolocation information!");
                    Assert.Exists(state.destBitmap.PropertyItems, prop => prop.Id == propId, "destBitmap did not copy geolocation information!");
                }
            }
        }

        private static ImageState CreateImageState(bool copyMetadata)
        {
            ImageState state = new ImageState(new ResizeSettings(), Size.Empty, false);

            if (copyMetadata)
            {
                state.settings["copymetadata"] = "true";
            }

            state.sourceBitmap = (Bitmap)Bitmap.FromFile(@"..\..\..\Samples\Images\gps-metadata-holder.jpg");
            state.destBitmap = new Bitmap(1, 1);

            return state;
        }

        private static RequestedAction CallProcessFinalBitmap(BuilderExtension plugin, ImageState state)
        {
            ITypeInfo type = Reflector.Wrap(plugin.GetType());

            IMethodInfo method = type.GetMethod("ProcessFinalBitmap", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Did not find 'ProcessFinalBitmap' method on plugin.");

            MethodInfo m = method.Resolve(false);
            Assert.IsNotNull(m);

            RequestedAction requestedAction = (RequestedAction)m.Invoke(plugin, new object[] { state });

            return requestedAction;
        }
    }
}
