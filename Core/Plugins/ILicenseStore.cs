using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins {
    /// <summary>
    /// Provides a place to cache/store licenses. Only responsible for licenses used by plugins attached to the current Config instance.
    /// </summary>
    public interface ILicenseStore:IPlugin {
        /// <summary>
        /// Returns a collection containing all licenses for the plugin's Config instance, in encrypted binary form.
        /// </summary>
        /// <returns></returns>
        ICollection<byte[]> GetLicenses();
        /// <summary>
        /// Stores the given licenses (excluding those present in web.config). 
        /// </summary>
        /// <param name="licenses">A collection of 'description' and 'encrypted binary license' pairs.</param>
        void SetLicenses(ICollection<KeyValuePair<string, byte[]>> licenses);
    }
}
