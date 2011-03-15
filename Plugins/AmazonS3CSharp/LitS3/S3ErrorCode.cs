
namespace LitS3
{
    /// <summary>
    /// Represents the possible error codes that can be returned by S3.
    /// </summary>
    /// <remarks>
    /// The list of codes was parsed from here:
    /// http://docs.amazonwebservices.com/AmazonS3/2006-03-01/ErrorCodeList.html
    /// </remarks>
    public enum S3ErrorCode
    {
        /// <summary>
        /// Represents a code unknown to this class.
        /// </summary>
        Unknown,
        /// <summary>
        /// Access Denied
        /// </summary>
        AccessDenied,
        /// <summary>
        /// There is a problem with your AWS account that prevents the operation from completing
        /// successfully. Please contact customer service at webservices@amazon.com.
        /// </summary>
        AccountProblem,
        /// <summary>
        /// The e-mail address you provided is associated with more than one account.
        /// </summary>
        AmbiguousGrantByEmailAddress,
        /// <summary>
        /// The Content-MD5 you specified did not match what we received.
        /// </summary>
        BadDigest,
        /// <summary>
        /// The requested bucket name is not available. The bucket namespace is shared by all users of
        /// the system. Please select a different name and try again.
        /// </summary>
        BucketAlreadyExists,
        /// <summary>
        /// Your previous request to create the named bucket succeeded and you already own it.
        /// </summary>
        BucketAlreadyOwnedByYou,
        /// <summary>
        /// The bucket you tried to delete is not empty.
        /// </summary>
        BucketNotEmpty,
        /// <summary>
        /// This request does not support credentials.
        /// </summary>
        CredentialsNotSupported,
        /// <summary>
        /// Cross location logging not allowed. Buckets in one geographic location cannot log
        /// information to a bucket in another location.
        /// </summary>
        CrossLocationLoggingProhibited,
        /// <summary>
        /// Your proposed upload is smaller than the minimum allowed object size.
        /// </summary>
        EntityTooSmall,
        /// <summary>
        /// Your proposed upload exceeds the maximum allowed object size.
        /// </summary>
        EntityTooLarge,
        /// <summary>
        /// The provided token has expired.
        /// </summary>
        ExpiredToken,
        /// <summary>
        /// You did not provide the number of bytes specified by the Content-Length HTTP header
        /// </summary>
        IncompleteBody,
        /// <summary>
        /// POST requires exactly one file upload per request.
        /// </summary>
        IncorrectNumberOfFilesInPostRequest,
        /// <summary>
        /// Inline data exceeds the maximum allowed size.
        /// </summary>
        InlineDataTooLarge,
        /// <summary>
        /// We encountered an internal error. Please try again.
        /// </summary>
        InternalError,
        /// <summary>
        /// The AWS Access Key Id you provided does not exist in our records.
        /// </summary>
        InvalidAccessKeyId,
        /// <summary>
        /// You must specify the Anonymous role.
        /// </summary>
        InvalidAddressingHeader,
        /// <summary>
        /// Invalid Argument
        /// </summary>
        InvalidArgument,
        /// <summary>
        /// The specified bucket is not valid.
        /// </summary>
        InvalidBucketName,
        /// <summary>
        /// The Content-MD5 you specified was an invalid.
        /// </summary>
        InvalidDigest,
        /// <summary>
        /// The specified location constraint is not valid.
        /// </summary>
        InvalidLocationConstraint,
        /// <summary>
        /// All access to this object has been disabled.
        /// </summary>
        InvalidPayer,
        /// <summary>
        /// The content of the form does not meet the conditions specified in the policy document.
        /// </summary>
        InvalidPolicyDocument,
        /// <summary>
        /// The requested range cannot be satisfied.
        /// </summary>
        InvalidRange,
        /// <summary>
        /// The provided security credentials are not valid.
        /// </summary>
        InvalidSecurity,
        /// <summary>
        /// The SOAP request body is invalid.
        /// </summary>
        InvalidSOAPRequest,
        /// <summary>
        /// The storage class you specified is not valid.
        /// </summary>
        InvalidStorageClass,
        /// <summary>
        /// The target bucket for logging does not exist, is not owned by you, or does not have the
        /// appropriate grants for the log-delivery group.
        /// </summary>
        InvalidTargetBucketForLogging,
        /// <summary>
        /// The provided token is malformed or otherwise invalid.
        /// </summary>
        InvalidToken,
        /// <summary>
        /// Couldn't parse the specified URI.
        /// </summary>
        InvalidURI,
        /// <summary>
        /// Your key is too long.
        /// </summary>
        KeyTooLong,
        /// <summary>
        /// The XML you provided was not well-formed or did not validate against our published schema.
        /// </summary>
        MalformedACLError,
        /// <summary>
        /// The XML you provided was not well-formed or did not validate against our published schema.
        /// </summary>
        MalformedXML,
        /// <summary>
        /// Your request was too big.
        /// </summary>
        MaxMessageLengthExceeded,
        /// <summary>
        /// Your POST request fields preceding the upload file were too large.
        /// </summary>
        MaxPostPreDataLengthExceededError,
        /// <summary>
        /// Your metadata headers exceed the maximum allowed metadata size.
        /// </summary>
        MetadataTooLarge,
        /// <summary>
        /// The specified method is not allowed against this resource.
        /// </summary>
        MethodNotAllowed,
        /// <summary>
        /// A SOAP attachment was expected, but none were found.
        /// </summary>
        MissingAttachment,
        /// <summary>
        /// You must provide the Content-Length HTTP header.
        /// </summary>
        MissingContentLength,
        /// <summary>
        /// The SOAP 1.1 request is missing a security element.
        /// </summary>
        MissingSecurityElement,
        /// <summary>
        /// Your request was missing a required header.
        /// </summary>
        MissingSecurityHeader,
        /// <summary>
        /// There is no such thing as a logging status sub-resource for a key.
        /// </summary>
        NoLoggingStatusForKey,
        /// <summary>
        /// The specified bucket does not exist.
        /// </summary>
        NoSuchBucket,
        /// <summary>
        /// The specified key does not exist.
        /// </summary>
        NoSuchKey,
        /// <summary>
        /// A header you provided implies functionality that is not implemented.
        /// </summary>
        NotImplemented,
        /// <summary>
        /// Your account is not signed up for the Amazon S3 service. You must sign up before you can
        /// use Amazon S3. You can sign up at the following URL: http://aws.amazon.com/s3
        /// </summary>
        NotSignedUp,
        /// <summary>
        /// A conflicting conditional operation is currently in progress against this resource.
        /// Please try again.
        /// </summary>
        OperationAborted,
        /// <summary>
        /// The bucket you are attempting to access must be addressed using the specified endpoint.
        /// Please send all future requests to this endpoint.
        /// </summary>
        PermanentRedirect,
        /// <summary>
        /// At least one of the pre-conditions you specified did not hold.
        /// </summary>
        PreconditionFailed,
        /// <summary>
        /// Temporary redirect.
        /// </summary>
        Redirect,
        /// <summary>
        /// Bucket POST must be of the enclosure-type multipart/form-data.
        /// </summary>
        RequestIsNotMultiPartContent,
        /// <summary>
        /// Your socket connection to the server was not read from or written to within the 
        /// timeout period.
        /// </summary>
        RequestTimeout,
        /// <summary>
        /// The difference between the request time and the server's time is too large.
        /// </summary>
        RequestTimeTooSkewed,
        /// <summary>
        /// Requesting the torrent file of a bucket is not permitted.
        /// </summary>
        RequestTorrentOfBucketError,
        /// <summary>
        /// The request signature we calculated does not match the signature you provided.
        /// Check your AWS Secret Access Key and signing method. For more information, see
        /// Authenticating REST Requests and Authenticating SOAP Requests for details.
        /// </summary>
        SignatureDoesNotMatch,
        /// <summary>
        /// Please reduce your request rate.
        /// </summary>
        SlowDown,
        /// <summary>
        /// You are being redirected to the bucket while DNS updates.
        /// </summary>
        TemporaryRedirect,
        /// <summary>
        /// The provided token must be refreshed.
        /// </summary>
        TokenRefreshRequired,
        /// <summary>
        /// You have attempted to create more buckets than allowed.
        /// </summary>
        TooManyBuckets,
        /// <summary>
        /// This request does not support content.
        /// </summary>
        UnexpectedContent,
        /// <summary>
        /// The e-mail address you provided does not match any account on record.
        /// </summary>
        UnresolvableGrantByEmailAddress,
        /// <summary>
        /// The bucket POST must contain the specified field name. If it is specified, please check
        /// the order of the fields.
        /// </summary>
        UserKeyMustBeSpecified
    }
}
