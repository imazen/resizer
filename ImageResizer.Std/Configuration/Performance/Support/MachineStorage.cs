using ImageResizer.Configuration.Issues;
using ImageResizer.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Configuration.Performance
{
    class MachineStorage: IIssueProvider
    {
        public MachineStorage()
        {
            sink = new IssueSink("MachineStorage");
            store = new Lazy<MultiFolderStorage>(() =>
                new MultiFolderStorage("MachineStorage", "file", sink,
                GetMachineWideFolders().ToArray(), FolderOptions.CreateIfMissing));
        }

        readonly IssueSink sink;
        readonly Lazy<MultiFolderStorage> store;

        public MultiFolderStorage Store => store.Value;

        static IEnumerable<string> GetMachineWideStorageLocations()
        {
            yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolderOption.Create);
            yield return Path.GetTempPath();
        }

        static IEnumerable<string> GetMachineWideFolders()
        {
            return GetMachineWideStorageLocations().Select(p => Path.Combine(p, "Imazen", "machine"));
        }

        public IEnumerable<IIssue> GetIssues()
        {
            return sink.GetIssues();
        }
    }
}
