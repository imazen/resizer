using System;
using System.IO;
using ImageResizer;
using Imageflow.Fluent;
using ImageResizer.Configuration;
using ImageResizer.Util;
using System.Linq;

namespace ImageResizer.Plugins.Imageflow.Watermarks
{
    /// <summary>
    /// Loads and preprocesses watermark images
    /// </summary>
    internal class WatermarkLoader
    {
        private readonly Config _config;

        public WatermarkLoader(Config config)
        {
            _config = config;
        }
        

        /// <summary>
        /// Loads a watermark image with preprocessing
        /// </summary>
        public IBytesSource LoadWatermarkImage(ImageBuilder fileProvider, string path, Instructions preprocessing)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            try
            {
                var disposeOfStream = true;
                var stream = fileProvider.GetStreamFromSource(path, new ResizeSettings(), ref disposeOfStream, out var _, out var restoreStreamPosition);
                if (restoreStreamPosition)
                {
                    throw new Exception("Restore stream position is not supported - HttpPostedFile and HttpPostedFileBase cannot be watermark sources.");
                }
                if (stream == null)
                    return null;
                var streamSource = new StreamSource(stream, disposeOfStream);
                try{
                    if (preprocessing != null && preprocessing.Count > 0)
                    {
                        
                        return PreprocessWatermark(streamSource, preprocessing, path);
                    }
                    return streamSource;
                }catch(Exception ex){
                    //_config.Pipeline.LogImageError(ex, "Failed to load watermark image: " + path);
                    streamSource.Dispose();
                    throw ex;
                }
                
            }
            catch (Exception ex)
            {
                //_config.Pipeline.LogImageError(ex, "Failed to load watermark image: " + path);
                throw ex;
            }
        }

        /// <summary>
        /// Preprocesses a watermark image using Imageflow
        /// </summary>
        private IBytesSource PreprocessWatermark(StreamSource source, Instructions preprocessing, string path)
        {
            
            try
            {
                using (var imageflowJob = new global::Imageflow.Fluent.ImageJob())
                {
                    var commandString = preprocessing.ToQueryString().Trim('?');
                    
                    var result = imageflowJob
                        .BuildCommandString(source, new BytesDestination(), commandString)
                        .Finish()
                        .InProcessAsync();

                    var encodeResult = result.Result.EncodeResults.First();
                    var bytes = encodeResult.TryGetBytes();
                    
                    if (bytes?.Array == null)
                        return null;

                    return new BytesSource(bytes.Value.Array, bytes.Value.Offset, bytes.Value.Count);
                }
            }
            catch (Exception ex)
            {
                source.Dispose();
                //_config.Pipeline.Log  ImageError(ex, "Failed to preprocess watermark image " + path + " with preprocessing " + preprocessing.ToQueryString());
                throw ex;
            }
        }
    }
} 