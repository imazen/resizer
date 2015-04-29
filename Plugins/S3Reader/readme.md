Tags: plugin
Edition: performance
Tagline: Process and resize images located on a remote Amazon S3 bucket. A VirtualPathProvider. Works best when combined with DiskCache.
Aliases: /plugins/s3reader


# S3Reader plugin

Deprecated. Use [S3Reader2 for compatibility with AWSSDK 2.0 and higher](/plugins/s3reader2)

Allows images located on Amazon S3 to be processed and resized as if they were located locally on the disk. Also serves files located on S3 - not restricted to images (unless vpp="false") is used.

NOTE: For European bucket support, you must set useSubdomains="true" and use V3.1.3 or higher.

### Example URLs

* http://localhost/s3/bucket-name/filename.jpg?width=100
* http://localhost/s3/bucket-name/folder/filename.jpg?width=100


See Samples/S3ReaderSample/ in the download for a sample project.


### Features

* Fast - no unnecessary http requests
* Offers mode for checking for newer files on a configured interval (useful when combined with disk caching)
* Works great with the DiskCache and CloudFront caching plugins
* Has an optional ssl mode
* Can be configured to access private bucket files with an access key


## Installation

Either run `Install-Package ImageResizer.Plugins.S3Reader` in the NuGet package manager, or:

1. Add ImageResizer.Plugins.S3Reader.dll to your project.
2. Add `<add name="S3Reader" buckets="my-bucket-1,my-bucket-2,my-bucket-3"/>` inside `<plugins></plugins>` in Web.config.


## Configuration

You must specify a comma-delimited list of permitted bucket names that can be accessed.

If you want to access non-public bucket items, you will need to specify an access ID and key.

  <add name="S3Reader" vpp="true" buckets="my-bucket-1,my-bucket-2,my-bucket-3" prefix="~/s3/"
   checkForModifiedFiles="false" useSsl="false" accessKeyId="" secretAccessKey="" useSubdomains="false" />

* buckets (required) - Comma-delimited list of permitted bucket names that can be accessed.
* prefix - the virtual folder that all buckets can be accessed under. Defaults to ~/s3/
* checkForModifiedFiles - If true, S3Reader will check for updated source files on S3 when a cached file is requested. The metadata is cached for an hour after it is last accessed (configurable by code).
  If false, S3 will never be checked for newer versions of cached files, reducing latency costs by 50%. Defaults to false.
* useSsl - Defaults to false. Set to true to transfer the image data over an encrypted connection. Slows things down significantly.
* accessKeyId, secretAccessKey - Use these if you need to access non-public files in your amazon buckets.
* vpp - Set to false to only serve image URLs that have a querystring.
* useSubdomains - Set to true to use the newer Amazon S3 bucket syntax, which is required for non-US bucket support. Defaults to false for compatibility reasons.



## Notes on bucket naming

When creating a bucket, you should avoid certain characters to ensure that DNS works properly. While Amazon will let you create buckets that violate some of these rules, you may have trouble accessing them using the subdomain syntax or over HTTPS. Make sure every bucket is also a valid DNS address. [Read more](http://wiki.ohnosequences.com/cloud_computing/aws/s3/bucket).

* Bucket names should not contain upper case letters
* Bucket names should not contain underscores (_)
* Bucket names should not end with a dash
* Bucket names should be between 3 and 63 characters long
* Bucket names cannot contain dashes next to periods (e.g., "my-.bucket.com" and "my.-bucket" are invalid)
* Bucket names cannot contain periods - Amazon states this is not supported for SSL-secured access, due to DNS complications. Your mileage may vary.



