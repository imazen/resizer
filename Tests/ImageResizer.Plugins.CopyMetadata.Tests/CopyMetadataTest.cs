using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Text;
using ImageResizer;
using ImageResizer.Plugins;
using ImageResizer.Plugins.CopyMetadata;
using ImageResizer.Resizing;
using Xunit;
using Xunit.Abstractions;

namespace ImageResizer.Plugins.CopyMetadata.Tests
{

    public class CopyMetadataTest
    {
        [Fact]
        public void SupportsCopyMetadataQuerystring()
        {
            CopyMetadataPlugin plugin = new CopyMetadataPlugin();

            plugin.GetSupportedQuerystringKeys().Contains("copymetadata");
        }

        [Fact]
        public void ShouldCopyMetadataWhenRequested()
        {
            CopyMetadataPlugin plugin = new CopyMetadataPlugin();

            using (ImageState state = CreateImageState(true))
            {
                RequestedAction requestedAction = CallProcessFinalBitmap(plugin, state);
                Assert.Equal(RequestedAction.None, requestedAction);

                Assert.NotEmpty(state.destBitmap.PropertyIdList);
                Assert.NotEmpty(state.destBitmap.PropertyItems);

                // Ensure that all the properties came from the original image...
                foreach (PropertyItem prop in state.destBitmap.PropertyItems)
                {
                    PropertyItem sourceProp = state.sourceBitmap.PropertyItems.SingleOrDefault(p => p.Id == prop.Id);
                    Assert.NotNull(sourceProp);//, "destBitmap ended up with a property that sourceBitmap didn't have!");
                   
                    Assert.Equal(sourceProp.Len, prop.Len);
                    Assert.Equal(sourceProp.Type, prop.Type);
                    Assert.Equal(sourceProp.Len, prop.Len);
                    Assert.Equal(sourceProp.Value, prop.Value);
                }
            }
        }

        [Fact]
        public void ShouldNotCopyMetadataWhenNotRequested()
        {
            CopyMetadataPlugin plugin = new CopyMetadataPlugin();

            using (ImageState state = CreateImageState(false))
            {
                RequestedAction requestedAction = CallProcessFinalBitmap(plugin, state);
                Assert.Equal(RequestedAction.None, requestedAction);

                Assert.Empty(state.destBitmap.PropertyIdList);
                Assert.Empty(state.destBitmap.PropertyItems);
            }
        }

        [Fact]
        public void ShouldNeverCopyExcludedProperties()
        {
            CopyMetadataPlugin plugin = new CopyMetadataPlugin();

            using (ImageState state = CreateImageState(true))
            {
                RequestedAction requestedAction = CallProcessFinalBitmap(plugin, state);
                Assert.Equal(RequestedAction.None, requestedAction);

                // Make sure some of properties were stripped...
                
                // PropertyTagOrientation
                Assert.True(state.sourceBitmap.PropertyItems.Any( prop => prop.Id == 0x0112));
                Assert.None(state.destBitmap.PropertyItems, prop => prop.Id == 0x0112);

                // PropertyTagXResolution
                Assert.True(state.sourceBitmap.PropertyItems.Any(prop => prop.Id == 0x011A));
                Assert.None(state.destBitmap.PropertyItems, prop => prop.Id == 0x011A);

                // PropertyTagYResolution
                Assert.Single(state.sourceBitmap.PropertyItems, prop => prop.Id == 0x011B);
                Assert.None(state.destBitmap.PropertyItems, prop => prop.Id == 0x011B);
            }
        }

        [Fact]
        public void CopiedAndOriginalMetadataIncludesGeolocationInformation()
        {
            CopyMetadataPlugin plugin = new CopyMetadataPlugin();

            using (ImageState state = CreateImageState(true))
            {
                RequestedAction requestedAction = CallProcessFinalBitmap(plugin, state);
                Assert.Equal(RequestedAction.None, requestedAction);

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
                    Assert.Single(state.sourceBitmap.PropertyItems, prop => prop.Id == propId);//, "sourceBitmap did not include geolocation information!");
                    Assert.Single(state.destBitmap.PropertyItems, prop => prop.Id == propId);//, "destBitmap did not copy geolocation information!");
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
            var type = plugin.GetType();

            var method = type.GetMethod("ProcessFinalBitmap", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);//, "Did not find 'ProcessFinalBitmap' method on plugin.");


            RequestedAction requestedAction = (RequestedAction)method.Invoke(plugin, new object[] { state });

            return requestedAction;
        }
    }
}
