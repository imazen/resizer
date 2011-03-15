
namespace LitS3
{
    /// <summary>
    /// Indicates the accessibility of a bucket.
    /// </summary>
    public enum BucketAccess
    {
        /// <summary> The bucket is owned and accessible by you. </summary>
        Accessible,
        
        /// <summary> The bucket is owned by someone else. </summary>
        NotAccessible,

        /// <summary> The bucket does not exist. </summary>
        NoSuchBucket
    }
}
