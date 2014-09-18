using ImageResizer.Configuration;
using ImageResizer.Plugins;
using ImageResizer.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace ImageResizer
{
    public class ImageUploadHelper
    {
        Config c;
        public ImageUploadHelper(Config c)
        {
            this.c = c;
        }


        public static ImageUploadHelper Current { get { return new ImageUploadHelper(Config.Current); } }

        /// <summary>
        /// Parses the file extension from the path and returns it. 
        /// If it contains a multi-segment extension like .txt.zip, only "zip" will be returned.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string GetExtension(string path)
        {
            int lastDot = path.LastIndexOfAny(new char[] { '.', '/', ' ', '\\', '?', '&', ':' });
            if (lastDot > -1 && path[lastDot] == '.') return path.Substring(lastDot + 1);
            else return null;
        }

        /// <summary>
        /// Lowercases and normalizes some common extension aliases (jpeg->jpg, tiff-tif). Does not filter out non-image extensions!
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public string NormalizeExtension(string extension)
        {
            if (extension == null) return null;
            extension = extension.ToLowerInvariant();
  
            var mapping = new Dictionary<string,string>{
                {"jpeg","jpg"},
                {"jpe","jpg"},
                {"jif","jpg"},
                {"jfif","jpg"},
                {"jfi","jpg"},
                {"exif","jpg"},
                {"tiff","tif"},
                {"tff","tif"},
            };

            return mapping.ContainsKey(extension) ? mapping[extension] : extension;
        }


        /// <summary>
        /// Returns true if the given extension is whitelisted. 
        /// </summary>
        /// <param name="ext"></param>
        /// <param name="whitelistedFormats">You can provide your own whitelist of extensions if desired (new string[]{"jpg","png"}).  This defaults to the extensions supported by the currently installed set of plugins.</param>
        /// <returns></returns>
        public bool IsExtensionWhitelisted(string ext, string[] whitelistedFormats = null)
        {
            if (whitelistedFormats == null)
                return c.Pipeline.IsAcceptedImageType("." + ext);
            else
            {
                foreach (var f in whitelistedFormats)
                {
                    if (ext.Equals(f, StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            }
        }


        /// <summary>
        /// Uses stream signatures first, then the original path, to detect the appropriate normalized image extension. Returns null if extension (or no whitelisted extension) is found.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="originalPath"></param>
        /// <param name="whitelistedFormats">You can provide your own whitelist of extensions if desired (new string[]{"jpg","png"}).  This defaults to the extensions supported by the currently installed set of plugins.</param>
        /// <returns></returns>
        public string GetWhitelistedExtension(Stream image = null, string originalPath = null, string[] whitelistedFormats = null)
        {   
            FileSignature sig = null;
            if (image != null && image.CanSeek && image.CanRead)
                sig = GuessFileTypeBySignature(image);

            var sigExt = sig != null ? NormalizeExtension(sig.PrimaryFileExtension) : null;

            if (sigExt != null && IsExtensionWhitelisted(sigExt,whitelistedFormats)) return sigExt;

            //Falback to untrusted ppath
            if (originalPath != null){
                string ext = NormalizeExtension(GetExtension(originalPath));

                if (ext != null & IsExtensionWhitelisted(ext,whitelistedFormats)) return ext;
            }
            return null;

        }

        /// <summary>
        /// Returns true if the uploaded file is identified as an image (looks for the image signature, falling back to the user-provided filename). 
        /// </summary>
        /// <param name="uploadFile">Must be an HttpPostedFile or another class with both FileName and InputStream members </param>
        /// <param name="whitelistedFormats">You can provide your own whitelist of extensions if desired (new string[]{"jpg","png"}).  This defaults to the extensions supported by the currently installed set of plugins.</param>
        /// <returns></returns>
        public bool IsUploadedFileAnImage(object uploadFile, string[] whitelistedFormats = null)
        {
            if (uploadFile == null) return false;

            PropertyInfo pname = uploadFile.GetType().GetProperty("FileName", typeof(string));
            PropertyInfo pstream = uploadFile.GetType().GetProperty("InputStream");

            if (pname != null && pstream != null)
            {
                var path = pname.GetValue(uploadFile, null) as string;
                var s = pstream.GetValue(uploadFile, null) as Stream;
                if (s == null && path == null) throw new ArgumentException("The given upload file has a null .InputStream and .FileName");

                return GetWhitelistedExtension(s, path, whitelistedFormats) != null;
            }
            return false;
        }

        /// <summary>
        /// Returns the [width,height] of the first frame/page of the image. May return null or throw exceptions.
        /// </summary>
        /// <param name="s">May be an UploadFile, a seekable Stream, a physical path, or a virtual path to the image.</param>
        /// <returns></returns>
        public int[] GetImageSize(object s){
            var j = new ImageJob(s,null);
            j.ResetSourceStream = true;
            j.DisposeSourceObject = false;
            c.CurrentImageBuilder.Build(j);
            return new int[]{j.SourceWidth.Value,j.SourceHeight.Value};
        }

        /// <summary>
        /// Returns the name used for the file, without any path information. Name will be in "guid.ext" form. Will create any intermediate directories required.
        /// </summary>
        /// <param name="baseDir"></param>
        /// <param name="uploadFile"></param>
        /// <param name="unrecognizedImageExtension"></param>
        /// <param name="whitelistedFormats"></param>
        /// <returns></returns>
        public string SaveUploadedFileSafely(string baseDir, object uploadFile, string unrecognizedImageExtension = ".unknown", string[] whitelistedFormats = null)
        {
            PropertyInfo pname = uploadFile.GetType().GetProperty("FileName", typeof(string));
            PropertyInfo pstream = uploadFile.GetType().GetProperty("InputStream");

            if (pname == null || pstream == null) throw new ArgumentException("uploadFile.InputStream and uploadFile.fileName are required. Ensure you are passing in an HttpPostedFile instance or similar");
            
            var uploadPath = pname.GetValue(uploadFile, null) as string;
            var uploadStream = pstream.GetValue(uploadFile, null) as Stream;

            var name = GenerateSafeImageName(uploadStream, uploadPath, unrecognizedImageExtension, whitelistedFormats);

            var dir = PathUtils.MapPathIfAppRelative(baseDir);

            var finalpath = Path.Combine(dir, name);

            string dirName = Path.GetDirectoryName(finalpath);
            if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);

            using (var fileStream = new FileStream(finalpath, FileMode.Create))
            {
                ImageResizer.ExtensionMethods.StreamExtensions.CopyToStream(uploadStream, fileStream);
                fileStream.Flush();
            }
            return name;
        }

        /// <summary>
        /// Generates a safe name for your image in the form "guid.ext". Uses stream signatures first, then the path to determine the appropriate image extension.
        /// You can provide your own whitelist of extensions if desired; this defaults to the extensions supported by the currently installed set of plugins.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="originalPath"></param>
        /// <param name="whitelistedFormats"></param>
        /// <param name="unrecognizedImageExtension">Pass null to have an ArgumentException thrown if the image type is not recognized as a whitelisted format</param>
        /// <returns></returns>
        public string GenerateSafeImageName(Stream image = null, string originalPath = null, string unrecognizedImageExtension = ".unknown", string[] whitelistedFormats = null)
        {   
            var ext = GetWhitelistedExtension(image,originalPath,whitelistedFormats) ?? unrecognizedImageExtension;
            if (ext == null) throw new ArgumentException("The provided image type is not recognized as a whitelisted format");
            return Guid.NewGuid().ToString("N", NumberFormatInfo.InvariantInfo) + ext;
        }

        /// <summary>
        /// Tries to guess the file type of the given stream by the byte signature. Typically more reliable than file extensions - users constantly rename file extensions expecting it to change the actual encoding.
        /// Throws Argument exception if stream isn't seekable and readable. Returns null if there were no matches.
        /// Make sure the current position of the stream is at the beginning of the file, or you will get no results. 
        /// Returns the stream to its original position
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public FileSignature GuessFileTypeBySignature(Stream s)
        {
            if (!(s.CanRead && s.CanSeek)) throw new ArgumentException("Stream must be seekable in order to guess the file type");

            List<FileSignature> signatures = new List<FileSignature>();
            foreach (var p in c.Plugins.GetAll<IFileSignatureProvider>())
                signatures.AddRange(p.GetSignatures());
            
            if (signatures.Count == 0) return null; //No signatures to compare!!

            //Sort by length, longest first
            signatures.Sort(delegate(FileSignature a, FileSignature b){
                return b.Signature.Length.CompareTo(a.Signature.Length);
            });
            //Copy the longest signature we may need to compare
            byte[] buffer = new byte[signatures[0].Signature.Length];
            int bytesRead = s.Read(buffer,0,buffer.Length);
            s.Seek(bytesRead, SeekOrigin.Current);

            foreach (var sig in signatures)
            {
                if (bytesRead < sig.Signature.Length) continue; //Signature longer than file
                if (sig.Signature.Length < 1) continue; //Empty signature
                bool matches = true;
                for (int i = 0; i < sig.Signature.Length; i++){
                    if (sig.Signature[i] != buffer[i])
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches)
                {
                    return sig;
                }
            }
            return null;
        }

    }
}
