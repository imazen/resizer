# ImageResizer.Plugins.S3Reader2


Allows images located on Amazon S3 to be processed and resized as if they were located locally on the disk.


### Example URLs

* http://localhost/s3/bucket-name/filename.jpg?width=100
* http://localhost/s3/bucket-name/folder/filename.jpg?width=100


See Samples/S3ReaderSample/ in the download for a sample project.

### Features

* Fast - no unnecessary HTTP requests
* Offers mode for checking for newer files on a configured interval (useful when combined with disk caching)
* Works great with the DiskCache and CloudFront caching plugins
* Has an optional SSL mode
* Can be configured to access private bucket files with an access key


## Installation

1. Either `Install-Package ImageResizer.Plugins.S3Reader2` or add a reference to ImageResizer.Plugins.S3Reader2.dll.
2. Add `<add name="S3Reader2" prefix="~/s3" region="us-east-1" buckets="my-bucket-1,my-bucket-2,my-bucket-3" />` inside `<plugins></plugins>` in Web.config.

## Region IDs

The default region is 'us-east-1', which works for buckets in both 'us-east-1' and 'us-west-1', but not 'us-west-2' or any other buckets.
Use multiple installations of S3Reader2 with different prefixes if you need to support multiple regions.

        "us-east-1", "US East (Virginia)"
        "us-west-1", "US West (N. California)"
        "us-west-2", "US West (Oregon)"
        "eu-west-1", "EU West (Ireland)"
        "ap-northeast-1", "Asia Pacific (Tokyo)"
        "ap-southeast-1", "Asia Pacific (Singapore)"
        "ap-southeast-2", "Asia Pacific (Sydney)"
        "sa-east-1", "South America (S�o Paulo)"
        "us-gov-west-1", "US GovCloud West (Oregon)"



## Notes on bucket naming

When creating a bucket, you should avoid certain characters to ensure that DNS works properly. While Amazon will let you create buckets that violate some of these rules, you may have trouble accessing them using the subdomain syntax or over HTTPS. Make sure every bucket is also a valid DNS address. [Read more](http://wiki.ohnosequences.com/cloud_computing/aws/s3/bucket).

* Bucket names should not contain upper case letters
* Bucket names should not contain underscores (_)
* Bucket names should not end with a dash
* Bucket names should be between 3 and 63 characters long
* Bucket names cannot contain dashes next to periods (e.g., "my-.bucket.com" and "my.-bucket" are invalid)
* Bucket names cannot contain periods - Amazon states this is not supported for SSL-secured access, due to DNS complications. Your mileage may vary.

## Configuration

You must specify a comma-delimited list of permitted bucket names that can be accessed.

If you want to access non-public bucket items, you will need to specify an access ID and key.

<add name="S3Reader2" buckets="my-bucket-1,my-bucket-2,my-bucket-3" prefix="~/s3/"
checkForModifiedFiles="false" useSsl="false" accessKeyId="" secretAccessKey="" region="us-east-1" />

* buckets (required) - Comma-delimited list of permitted bucket names that can be accessed.
* region - Set to the region containing your buckets.
* useSsl - Defaults to false. Set to true to transfer the image data over an encrypted connection. Decreases performance.
* accessKeyId, secretAccessKey - Use these if you need to access non-public files in your amazon buckets.
