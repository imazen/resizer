using ImageResizer.Configuration.Issues;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace ImageResizer.Configuration.Performance
{
    /// <summary>
    /// Collects digest(mac addresses), processor count, bitness, network/fixed/other drive count, 
    /// and [filesystem,available gigs,total gigs] for local drives. 
    /// </summary>
    class HardwareInfo
    {
        public struct FixedDriveInfo
        {
            public long TotalBytes;
            public long AvailableBytes;
            public string Filesystem;
        }
        public string MachineDigest { get; }
        // Excludes other processor groups that aren't available to the CLR
        public int LogicalCores { get; }
        public bool OperatingSystem64Bit { get; }

        public int NetworkDrives { get; }
        public int OtherDrives { get; }
        public IEnumerable<FixedDriveInfo> FixedDrives { get; }

        public HardwareInfo(IIssueReceiver sink)
        {
            try
            {
                var sortedMacAddresses = NetworkInterface.GetAllNetworkInterfaces()
                        .Select(nic => nic.GetPhysicalAddress().ToString().ToLowerInvariant())
                        .OrderBy(s => s).ToArray();
                MachineDigest = Utilities.Sha256TruncatedBase64(string.Join("|", sortedMacAddresses), 16);
            }
            catch (NetworkInformationException e)
            {
                sink.AcceptIssue(new Issue("Failed to query network interface. Function not affected.", e.ToString(), IssueSeverity.Warning));
                MachineDigest = "none";
            }
            
            LogicalCores = Environment.ProcessorCount;
            OperatingSystem64Bit = Environment.Is64BitOperatingSystem;

            var appDriveRoot = Path.GetPathRoot(HostingEnvironment.ApplicationPhysicalPath ?? Environment.CurrentDirectory);
     
            var allDrives = DriveInfo.GetDrives();
            NetworkDrives = allDrives.Count(d => d.DriveType == DriveType.Network);
            OtherDrives = allDrives.Count(d => d.DriveType != DriveType.Network && d.DriveType != DriveType.Fixed);
            FixedDrives = allDrives.Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
                .Select(d => new FixedDriveInfo { Filesystem = d.DriveFormat + (d.Name == appDriveRoot ? "*" : ""), TotalBytes = d.TotalSize, AvailableBytes = d.AvailableFreeSpace }).ToArray();

            // TODO: cpu feature support
        }

        public void Add(IInfoAccumulator query)
        {
            var q = query.WithPrefix("h_");
            // Excludes other processor groups that aren't available to the CLR
            q.Add("logical_cores", LogicalCores.ToString());

            q.Add("mac_digest",this.MachineDigest);
            q.Add("os64", OperatingSystem64Bit);
            q.Add("network_drives_count", NetworkDrives);
            q.Add("other_drives_count", OtherDrives);
            q.Add("fixed_drives_count", FixedDrives.Count());
            foreach (var drive in FixedDrives)
            {

                q.Add("fixed_drive",
                    $"{drive.Filesystem},{drive.AvailableBytes / 1000000000},{drive.TotalBytes / 1000000000}");
            }
        }
    }
}
