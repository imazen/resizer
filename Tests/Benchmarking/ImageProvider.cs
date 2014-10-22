using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Bench
{
    public class ImageProvider
    {

        public ImageProvider()
        {
            ImageDict = new Dictionary<string, Tuple<int, int, string>>();
        }
        private string _cacheFolder = null;
        public string CacheFolder{
            get{
                return _cacheFolder ?? @".\bench_image_cache\";
            }
            set{
                _cacheFolder = value;
            }
        }

        List<string> RemoteImages = new List<string>();
        List<string> LocalImages = new List<string>();
        List<Tuple<int, int, string>> BlankImages = new List<Tuple<int, int, string>>();

        public ImageProvider AddRemoteImages(string commonPrefix, params string[] images){
            RemoteImages.AddRange(images.Select(s => commonPrefix + s));
            return this;
        }

        public ImageProvider AddBlankImages(IEnumerable<Tuple<int, int, string>> imageTypes)
        {
            BlankImages.AddRange(imageTypes);
            return this;
        }

        public ImageProvider AddLocalImages(string commonPrefix, params string[] images){
            LocalImages.AddRange(images.Select(s => Path.Combine(commonPrefix,s)));
            return this;
        }

        private string CachedPathFor(string uri){
            return Path.Combine(CacheFolder, Path.GetFileName(uri));
        }
        private string CachedPathFor(Tuple<int, int, string> blankImage)
        {
            return Path.Combine(CacheFolder, string.Format("blank_{0}x{1}.{2}",blankImage.Item1,blankImage.Item2,blankImage.Item3));
        }

        private ImageFormat ImageFormatFromExtension(string ext)
        {
            return ext == "png" ? ImageFormat.Png : ext == "gif" ? ImageFormat.Gif : ext == "tiff" ? ImageFormat.Tiff : ext == "bmp" ? ImageFormat.Bmp : ImageFormat.Jpeg;
        }
        public async Task PrepareImagesAsync()
        {
            var remainingRemoteImages = RemoteImages.Where(u => !File.Exists(CachedPathFor(u))).Select(async u =>
            {
                var r = await HttpWebRequest.CreateHttp(u).GetResponseAsync();
                var ms = new MemoryStream();
                await r.GetResponseStream().CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);
                using (var fs = OpenWriteCreateDirs(CachedPathFor(u)))
                {
                    await ms.CopyToAsync(fs);
                }
                return CachedPathFor(u);
            }).ToArray();

            var remainingBlankImages = BlankImages.Where(d => !File.Exists(CachedPathFor(d))).Select(async d =>
            {
                var ms = new MemoryStream();
                using (var b = new Bitmap(d.Item1, d.Item2))
                {
                    b.Save(ms, ImageFormatFromExtension(d.Item3)); //Saves at 100% quality, bad idea
                    ImageDict[CachedPathFor(d)] = d;
                }
                ms.Seek(0, SeekOrigin.Begin);
                using (var fs = OpenWriteCreateDirs(CachedPathFor(d)))
                {
                    await ms.CopyToAsync(fs);
                }
                return CachedPathFor(d);
            }).ToArray();

            await Task.WhenAll(remainingRemoteImages);
            await Task.WhenAll(remainingBlankImages);
        }

        private void CreateDirs(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (dir.Trim().Length == 0) return;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
        private FileStream OpenWriteCreateDirs(string path)
        {
            CreateDirs(path);
            return File.OpenWrite(path);
        }
        /// <summary>
        /// Returns a list of paths to all existing locally cached files (based on remote and blank image queries)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetImages()
        {
            return RemoteImages.Select(u => CachedPathFor(u)).Concat(BlankImages.Select(d => CachedPathFor(d)))
                .Concat(LocalImages).Where(p => File.Exists(p));   
        }
        public IEnumerable<Tuple<string,string>> GetImagesAndDescriptions()
        {
            return RemoteImages.Select(u => CachedPathFor(u)).Concat(BlankImages.Select(d => CachedPathFor(d)))
                .Concat(LocalImages).Where(p => File.Exists(p)).Select(p => new Tuple<string,string>(p,InfoString(p)));
        }


        public string InfoString(string path)
        {
            var d = Info(path);
            return Path.GetFileName(path) + " (" + d.Item1 + "x" + d.Item2 + ") (" + d.Item3 + ")";
        }

        Dictionary<string, Tuple<int, int, string>> ImageDict { get; set; }
        public Tuple<int, int, string> Info(string path)
        {
            if (!ImageDict.ContainsKey(path)) { 
                using (Bitmap b = new Bitmap(path))
                {
                    return new Tuple<int, int, string>(b.Width, b.Height, b.PixelFormat.ToString());
                }
            }
            return ImageDict[path];
        }
    }
}
