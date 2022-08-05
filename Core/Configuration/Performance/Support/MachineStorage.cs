using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageResizer.Configuration.Issues;
using ImageResizer.Plugins;

namespace ImageResizer.Configuration.Performance
{
    internal class MachineStorage : IIssueProvider
    {
        public MachineStorage()
        {
            sink = new IssueSink("MachineStorage");
            store = new Lazy<MultiFolderStorage>(() =>
                new MultiFolderStorage("MachineStorage", "file", sink,
                    GetMachineWideFolders().ToArray(), FolderOptions.CreateIfMissing));
        }

        private readonly IssueSink sink;
        private readonly Lazy<MultiFolderStorage> store;

        public MultiFolderStorage Store => store.Value;

        private static IEnumerable<string> GetMachineWideStorageLocations()
        {
            yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData,
                Environment.SpecialFolderOption.Create);
            yield return Path.GetTempPath();
        }

        private static IEnumerable<string> GetMachineWideFolders()
        {
            return GetMachineWideStorageLocations().Select(p => Path.Combine(p, "Imazen", "machine"));
        }

        public IEnumerable<IIssue> GetIssues()
        {
            return sink.GetIssues();
        }
    }
}