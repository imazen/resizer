using ImageResizer.Configuration;
using ImageResizer.Configuration.Issues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.Licensing
{
    public delegate void LicenseManagerEvent(ILicenseManager mgr);

    /// <summary>
    /// When multiple paid plugins and license keys are involved, this interface allows the deduplication of effort and centralized license access.
    /// </summary>
    public interface ILicenseManager : IIssueProvider
    {
        /// <summary>
        /// Persistent cache 
        /// </summary>
        IPersistentStringCache Cache { get; set; }

        /// <summary>
        ///  Must be called often to fetch remote licenses appropriately. Not resource intensive; call for every image request.
        /// </summary>
        void Heartbeat();

        /// <summary>
        /// When Heartbeat() was first called (i.e, first chance to process licenses)
        /// </summary>
        DateTimeOffset? FirstHeartbeat { get; }

        /// <summary>
        /// The license manager will add a handler to notice license changes on this config. It will also process current licenses on the config.
        /// </summary>
        /// <param name="c"></param>
        void MonitorLicenses(Config c);

        /// <summary>
        /// Subscribes itself to heartbeat events on the config
        /// </summary>
        /// <param name="c"></param>
        void MonitorHeartbeat(Config c);

        /// <summary>
        /// Register a license key (if it isn't already), and return the inital chain (or null, if the license is invalid)
        /// </summary>
        /// <param name="license"></param>
        /// <param name="access"></param>
        ILicenseChain GetOrAdd(string license, LicenseAccess access);

        /// <summary>
        /// Returns all shared license chains (a chain is shared if any relevant license is marked shared)
        /// </summary>
        /// <returns></returns>
        IEnumerable<ILicenseChain> GetSharedLicenses();

        /// <summary>
        /// Returns all license chains, both shared and private (for diagnostics/reporting)
        /// </summary>
        /// <returns></returns>
        IEnumerable<ILicenseChain> GetAllLicenses();

        /// <summary>
        /// Adds a weak-referenced handler to the LicenseChange event.
        /// </summary>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="target"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        LicenseManagerEvent AddLicenseChangeHandler<TTarget>(TTarget target, Action<TTarget, ILicenseManager> action);

        /// <summary>
        /// Removes the event handler created by AddLicenseChangeHandler
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        void RemoveLicenseChangeHandler(LicenseManagerEvent handler);
    }

    public interface ILicenseChain
    {
        /// <summary>
        /// Plan ID or domain name (lowercase invariant)
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Whether license chain is shared app-wide
        /// </summary>
        bool Shared { get; }

        /// <summary>
        /// If the license chain is updated over the internet
        /// </summary>
        bool IsRemote { get; }

        /// <summary>
        /// Can return fresh, cached, and inline licenes
        /// </summary>
        /// <returns></returns>
        IEnumerable<ILicenseBlob> Licenses();

        /// <summary>
        /// Returns null until a fresh license has been fetched (within process lifetime) 
        /// </summary>
        /// <returns></returns>
        ILicenseBlob FetchedLicense();

        ///// <summary>
        ///// Returns the (presumably) disk cached license
        ///// </summary>
        ///// <returns></returns>
        ILicenseBlob CachedLicense();


        /// <summary>
        /// Only returns information about licenses that are marked as public. Otherwise returns "License hidden"
        /// </summary>
        /// <returns></returns>
        string ToPublicString();
    }

    /// <summary>
    /// Provides license UTF-8 bytes and signature
    /// </summary>
    public interface ILicenseBlob
    {
        byte[] Signature();
        byte[] Data();
        string Original { get; }
        ILicenseDetails Fields();
    }

    public interface ILicenseDetails
    {
        string Id { get; }
        IReadOnlyDictionary<string, string> Pairs();
        string Get(string key);
        DateTimeOffset? Issued { get; }
        DateTimeOffset? Expires { get; }
        DateTimeOffset? SubscriptionExpirationDate { get; }
    }

    public interface ILicenseClock
    {
        DateTimeOffset GetUtcNow();
        long GetTimestampTicks();
        long TicksPerSecond { get; }

        DateTimeOffset? GetBuildDate();
        DateTimeOffset? GetAssemblyWriteDate();
    }

}
