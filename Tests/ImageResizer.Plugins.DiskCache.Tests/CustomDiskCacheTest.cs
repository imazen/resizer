using System;
using System.Collections.Generic;
using System.Text;
using Gallio.Framework;
using MbUnit.Framework;
using MbUnit.Framework.ContractVerifiers;
using System.IO;
using ImageResizer.Configuration.Logging;

namespace ImageResizer.Plugins.DiskCache.Tests {
    [TestFixture]
    [Row(0,50,false)]
    [Row(0,50,true)]
    //[Row(8000,100,true)]
    public class CustomDiskCacheTest :ILoggerProvider{

        public CustomDiskCacheTest(int subfolders, int totalFiles, bool hashModifiedDate) {
            char c = System.IO.Path.DirectorySeparatorChar;
            string folder = System.IO.Path.GetTempPath().TrimEnd(c) + c + System.IO.Path.GetRandomFileName();
            cache = new CustomDiskCache(this, folder,subfolders,hashModifiedDate);
            this.quantity = totalFiles;

            for (int i = 0; i < quantity;i++){
                cache.GetCachedFile(i.ToString(),"test",delegate(Stream s){
                    s.WriteByte(32); //Just one space
                },defaultDate, 10);
            }
        }
        int quantity;
        CustomDiskCache cache = null;
        DateTime defaultDate = new DateTime(2011, 1, 1);

        [ThreadedRepeat(10)]
        [Test(Order=1)]
        public void TestAccess() {
            CacheResult r = 
                cache.GetCachedFile(new Random().Next(0, quantity).ToString(), "test", 
                delegate(Stream s) { Assert.Fail("No files have been modified, this should not execute"); }, defaultDate, 100);

            Assert.IsTrue(System.IO.File.Exists(r.PhysicalPath));
            Assert.IsTrue(r.Result == CacheQueryResult.Hit);
        }

        volatile int seed = 0;
        [Test (Order=2)]
        [ThreadedRepeat(20)]
        public void TestUpdate() {
            //try to get a unique date time value
            DateTime newTime = DateTime.UtcNow.AddDays(seed++);
            CacheResult r =
                cache.GetCachedFile(new Random().Next(0, quantity).ToString(), "test",
                delegate(Stream s) {
                    s.WriteByte(32); //Just one space
                }, newTime, 100);

            Assert.AreEqual<DateTime>(newTime, System.IO.File.GetLastWriteTimeUtc(r.PhysicalPath));
            Assert.IsTrue(r.Result == CacheQueryResult.Miss);
        }


        [Test(Order=3)]
        public void TestClear() {
            System.IO.Directory.Delete(cache.PhysicalCachePath, true);
        }

        public ILogger Logger {
            get { return null; }
        }
    }
}
