﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace GoogleAnalytics
{
    public static class HttpProtocol
    {
        private const string BaseUrl = "https://www.google-analytics.com/mp/collect";
        private const string DebugBaseUrl = "https://www.google-analytics.com/debug/mp/collect";

        public static Task PostMeasurements(Analytics a)
        {
            return Post(BaseUrl, a);
        }

        public static async Task<ValidationResponse> ValidateMeasurements(Analytics a)
        {
            var response = await Post(DebugBaseUrl, a);
            if (response.Content != null)
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var responseSerializer = new DataContractJsonSerializer(typeof(ValidationResponse));
                    return (ValidationResponse)responseSerializer.ReadObject(stream);
                }
            }

            throw new Exception("No validation response");
        }

        private static async Task<HttpResponseMessage> Post(string baseUri, Analytics a)
        {
            const string guide = "\r\nSee https://developers.google.com/analytics/devguides/collection/protocol/ga4";

            if (a.Events.Count > 25)
            {
                throw new Exception("A maximum of 25 events can be specified per request." + guide);
            }

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                throw new Exception("A network interface is not available");
            }

            string query = a.ToQueryString();
            string url = baseUri + "?" + query;

            if (string.IsNullOrEmpty(a.UserId))
            {
                a.UserId = a.ClientId;
            }

            HttpClient client = new HttpClient();
            AddUserProperties(client, a);

            var settings = new DataContractJsonSerializerSettings
            {
                EmitTypeInformation = EmitTypeInformation.Never,
                UseSimpleDictionaryFormat = true,
                KnownTypes = GetKnownTypes(a)
            };
            var serializer = new DataContractJsonSerializer(typeof(Analytics), settings);

            byte[] bytes;
            
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, a);
                bytes = ms.ToArray();
                if (bytes.Length > 130000)
                {
                    throw new Exception("The total size of analytics payloads cannot be greater than 130kb bytes" + guide);
                }
            }

            var json = Encoding.UTF8.GetString(bytes);
            using (var jsonContent = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var response = await client.PostAsync(new Uri(url), jsonContent);
                response.EnsureSuccessStatusCode();
                return response;
            }
        }

        private static void AddUserProperties(HttpClient client, Analytics a)
        {
            string platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : "OSX");
            var arch = RuntimeInformation.OSArchitecture.ToString();
            client.DefaultRequestHeaders.Add("User-Agent", string.Format("Mozilla/5.0 ({0}; {1})", platform, arch));
            client.DefaultRequestHeaders.Add("Accept-Language", CultureInfo.CurrentCulture.Name);

            var cultureInfo = CultureInfo.CurrentCulture;
            var regionInfo = new RegionInfo(cultureInfo.LCID);
            var country = regionInfo.TwoLetterISORegionName;//regionInfo.DisplayName;

            a.UserProperties = new UserProperties()
            {
                FrameworkVersion = new UserPropertyValue(RuntimeInformation.FrameworkDescription),
                Platform = new UserPropertyValue(platform),
                PlatformVersion = new UserPropertyValue(RuntimeInformation.OSDescription),
                Language = new UserPropertyValue(cultureInfo.Name),
                Country = new UserPropertyValue(country),
            };
        }

        private static Type[] GetKnownTypes(Analytics a)
        {
            HashSet<Type> types = new HashSet<Type>();
            foreach (var e in a.Events)
            {
                types.Add(e.GetType());
            }
            return new List<Type>(types).ToArray();
        }
    }

    [DataContract]
    public class ValidationResponse
    {
        [DataMember(Name = "validationMessages")]
        public ValidationMessage[] ValidationMessages;
    }

    [DataContract]
    public class ValidationMessage
    {
        [DataMember(Name = "description")]
        public string Description;
        [DataMember(Name = "fieldPath")]
        public string InvalidFieldPath;
        [DataMember(Name = "validationCode")]
        public string ValidationCode;
    }
}
