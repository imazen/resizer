using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace LitS3
{
    class S3Authorizer : IComparer<string>
    {
        S3Service service;
        HMACSHA1 signer;

        public S3Authorizer(S3Service service)
        {
            this.service = service;
            this.signer = new HMACSHA1(Encoding.UTF8.GetBytes(service.SecretAccessKey));
        }

        public static bool IsAuthorized(HttpWebRequest request)
        {
            return request.Headers[HttpRequestHeader.Authorization] != null;
        }

        /// <summary>
        /// Signs the given HttpWebRequest using the HTTP Authorization header with a value
        /// generated using the contents of the request plus our SecretAccessKey.
        /// </summary>
        /// <remarks>
        /// See http://docs.amazonwebservices.com/AmazonS3/2006-03-01/RESTAuthentication.html
        /// </remarks>
        public void AuthorizeRequest(HttpWebRequest request, string bucketName)
        {
            request.Headers[S3Headers.AmazonDate] = DateTime.UtcNow.ToString("r");

            var stringToSign = new StringBuilder()
                .Append(request.Method).Append('\n')
                .Append(request.Headers[HttpRequestHeader.ContentMd5]).Append('\n')
                .Append(request.ContentType).Append('\n')
                .Append('\n'); // ignore the official Date header since WebRequest won't send it

            AppendCanonicalizedAmzHeaders(request, stringToSign);

            // append the resource WebRequested using amazon's CanonicalizedResource format

            // does this request address a bucket?
            if (service.UseSubdomains && bucketName != null)
                stringToSign.Append('/').Append(bucketName);

            stringToSign.Append(request.RequestUri.AbsolutePath);

            // add sub-resource, if present. "?acl", "?location", "?logging", or "?torrent"
            string query = request.RequestUri.Query;

            if (query == "?acl" || query == "?location" || query == "?logging" || query == "?torrent")
                stringToSign.Append(query);
            
            string signed = Sign(stringToSign.ToString());
            
            string authorization = string.Format("AWS {0}:{1}", service.AccessKeyID, signed);

            request.Headers[HttpRequestHeader.Authorization] = authorization;
        }

        public string AuthorizeQueryString(string bucketName, string key, DateTime expires)
        {
            var stringToSign = new StringBuilder()
                .Append("GET").Append('\n').Append('\n').Append('\n')
                .Append(expires.SecondsSinceEpoch()).Append('\n');

            // construct CanonicalizedResource, always /bucket/key
            stringToSign.Append('/').Append(bucketName);
            stringToSign.Append('/').Append(Uri.EscapeDataString(key));

            return Sign(stringToSign.ToString());
        }

        /// <summary>
        /// Implements string comparison for the purpose of sorting amazon request headers
        /// lexographically. The default string.Compare() is "interesting", in that it
        /// attempts to sort words according to current locale settings. What we want is
        /// the old-school sorting based on the numeric value of each char, which is
        /// what CompareOrdinal does.
        /// </summary>
        public int Compare(string x, string y)
        {
            return string.CompareOrdinal(x, y);
        }

        void AppendCanonicalizedAmzHeaders(HttpWebRequest request, StringBuilder stringToSign)
        {
            // specify ourself as the sorter so we can use string.CompareOrdinal.
            var amzHeaders = new SortedList<string, string[]>(this);

            foreach (string header in request.Headers)
                if (header.StartsWith(S3Headers.AmazonHeaderPrefix))
                    amzHeaders.Add(header.ToLower(), request.Headers.GetValues(header));

            // append the sorted headers in amazon's defined CanonicalizedAmzHeaders format
            foreach (var amzHeader in amzHeaders)
            {
                stringToSign.Append(amzHeader.Key).Append(':');

                // ensure that there's no space around the colon
                bool lastCharWasWhitespace = true;

                foreach (char c in string.Join(",", amzHeader.Value))
                {
                    bool isWhitespace = char.IsWhiteSpace(c);

                    if (isWhitespace && !lastCharWasWhitespace)
                        stringToSign.Append(' '); // amazon wants whitespace "folded" to a single space
                    else if (!isWhitespace)
                        stringToSign.Append(c);

                    lastCharWasWhitespace = isWhitespace;
                }

                stringToSign.Append('\n');
            }
        }

        string Sign(string stringToSign)
        {
            return Convert.ToBase64String(signer.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
        }
    }

    /// <summary>
    /// Assists in converting DateTime objects into the seconds-since-epoch required by some
    /// parts of S3.
    /// </summary>
    static class DateTimeEpochExtension
    {
        static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        public static long SecondsSinceEpoch(this DateTime date)
        {
            return (long)((date.ToUniversalTime() - Epoch).TotalSeconds);
        }
    }
}
