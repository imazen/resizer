namespace ImageResizer.Plugins.LicenseVerifier {
    /// <summary>
    /// The state of the domain/feature combination
    /// </summary>
    public enum FeatureState {
        /// <summary>
        /// A valid license is on-file for this feature/domain combo
        /// </summary>
        Enabled,
        /// <summary>
        /// This feature/domain combo has not yet been checked for licensing. 
        /// </summary>
        Pending,
        /// <summary>
        /// No valid licenses could be found for this feature/domain
        /// </summary>
        Rejected
    }
}
