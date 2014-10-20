using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins {
    /// <summary>
    /// Provides license verification and enforcement services. Do not access directly; use your local embedded static method to verify instance integrity.
    /// </summary>
    public interface ILicenseService:IPlugin {
        /// <summary>
        /// Notify the license service that the given feature is being used for the given domain. 
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="feature"></param>
        void NotifyUse(string domain, Guid feature);
        /// <summary>
        /// Configure the display name for the given feature id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="featureDisplayName"></param>
        void SetFriendlyName(Guid id, string featureDisplayName);
        /// <summary>
        /// Returns a changing shared secret to make interface hijacking difficult.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        long VerifyAuthenticity(Guid feature);

        /// <summary>
        /// Returns a plaintext report on licensing status
        /// </summary>
        /// <param name="forceVerification">If true, pending verifications will be completed before the method returns</param>
        /// <returns></returns>
        string GetLicensingOverview(bool forceVerification);
    }
}
