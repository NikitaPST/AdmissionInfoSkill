using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ElasticSearchIntegration
{
    public class Function
    {
        private readonly string ESADDRESS = Environment.GetEnvironmentVariable("EsAddress");
        private readonly string ACCESS_KEY = Environment.GetEnvironmentVariable("AccessKey");
        private readonly string SECRET_KEY = Environment.GetEnvironmentVariable("SecretKey");
        private const string INDEX_NAME = "universities/";
        private const string TYPE_NAME = "record/";
        private const string REGION = "us-west-2";

        /// <summary>
        /// A function to transfer data from dynamodb to elastic search index.
        /// </summary>
        /// <param name="input">User input.</param>
        /// <param name="context">Lambda context.</param>
        /// <returns>Success/Error message.</returns>
        public async Task<string> FunctionHandler(JObject input, ILambdaContext context)
        {
            try
            {
                int count = 0;
                Aws4RequestSigner.AWS4RequestSigner signer = new Aws4RequestSigner.AWS4RequestSigner(ACCESS_KEY, SECRET_KEY);
                JArray records = (JArray)input["Records"];
                foreach (var record in records)
                {
                    string universityName = (string)record["dynamodb"]["Keys"]["UniversityName"]["S"];
                    string eventName = (string)record["eventName"];
                    string id = ToMD5(universityName);
                    
                    if (eventName == "REMOVE")
                    {
                        HttpRequestMessage request = new HttpRequestMessage()
                        {
                            Method = HttpMethod.Delete,
                            RequestUri = new Uri(ESADDRESS + INDEX_NAME + TYPE_NAME + id)
                        };
                        request = await signer.Sign(request, "es", REGION);
                        using (HttpClient client = new HttpClient())
                        {
                            HttpResponseMessage response = await client.SendAsync(request);
                        }
                    }
                    else if ((eventName == "INSERT") || (eventName == "MODIFY"))
                    {
                        string content = string.Format(@"{{ ""name"": ""{0}"" }}", universityName);

                        HttpRequestMessage request = new HttpRequestMessage()
                        {
                            Method = HttpMethod.Put,
                            RequestUri = new Uri(ESADDRESS + INDEX_NAME + TYPE_NAME + id),
                            Content = new StringContent(content)
                        };
                        request = await signer.Sign(request, "es", REGION);
                        using (HttpClient client = new HttpClient())
                        {
                            HttpResponseMessage response = await client.SendAsync(request);
                        }
                    }
                    count++;
                }
                return $"{count} records processed.";
            }
            catch (Exception ex)
            {
                context.Logger.Log(ex.Message);
                return ex.Message;
            }
        }

        /// <summary>
        /// Converts string to MD5 hash.
        /// </summary>
        /// <param name="str">Input string.</param>
        /// <returns>String representation of MD5 hash.</returns>
        public string ToMD5(string str)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(str);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i=0; i<hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}
