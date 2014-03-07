using ImageResizer.Plugins.LicenseVerifier.Http;
using Should;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace ImageResizer.Plugins.LicenseVerifier.Tests.Integration.Http {
    public class HttpClientTests {

        private readonly HttpClient httpClient;

        public HttpClientTests() {
            httpClient = new HttpClient();
        }

        public void Should_be_able_to_make_a_get_request() {
            var url = new Uri("http://httpbin.org/get");
            var httpRequest = new HttpRequest {
                Url = url,
                Method = HttpMethod.Get
            };

            var httpResponse = httpClient.Send(httpRequest);
            httpResponse.StatusCode.ShouldEqual(HttpStatusCode.OK);
        }

        public void Should_be_able_to_make_a_post_request() {
            var url = new Uri("http://httpbin.org/post");
            var httpRequest = new HttpRequest {
                Url = url,
                Method = HttpMethod.Post,
                Content = "some-key=some-value",
                ContentType = "application/x-www-form-urlencoded"
            };

            var httpResponse = httpClient.Send(httpRequest);

            httpResponse.StatusCode.ShouldEqual(HttpStatusCode.OK);

            using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(httpResponse.Content))) {
                DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
                settings.UseSimpleDictionaryFormat = true;

                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(HttpBinResponse), settings);

                var httpBinResponse = (HttpBinResponse)serializer.ReadObject(ms);
                Dictionary<string, string> form = httpBinResponse.form;

                httpBinResponse.url.ShouldEqual("http://httpbin.org/post");
                httpBinResponse.form["some-key"].ShouldEqual("some-value");
            }
        }
    }

    [DataContract]
    public class HttpBinResponse {
        [DataMember]
        public string url { get; set; }

        [DataMember]
        public Dictionary<string, string> headers { get; set; }

        [DataMember]
        public Dictionary<string, string> form { get; set; }
    }
}
