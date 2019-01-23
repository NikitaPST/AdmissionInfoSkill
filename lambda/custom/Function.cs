using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;
using Newtonsoft.Json.Linq;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AdmissionInfoLambda
{
    public class Function
    {
        // NESTED TYPES

        /// <summary>
        /// Result of database search.
        /// </summary>
        class SearchResult
        {
            public string UniversityName { get; set; }
            public string Value { get; set; }
            public string ImageLink { get; set; }
        }

        // CONSTANTS
        private const string INDEX_NAME = "universities/";
        private const string LAST_SEARCH_KEY = "last_search";
        private const string LAST_INTENT_KEY = "last_intent";
        private const string LOCALENAME = "LOCALE";
        private const string REGION = "us-west-2";
        private const string STATE_KEY = "state";
        private const string TABLE_NAME = "Universities";
        private const string US_LOCALE = "en-US";
        private readonly string ESADDRESS = Environment.GetEnvironmentVariable("EsAddress");
        private readonly string ACCESS_KEY = Environment.GetEnvironmentVariable("AccessKey");
        private readonly string SECRET_KEY = Environment.GetEnvironmentVariable("SecretKey");

        // FIELDS
        private ILambdaContext context = null;
        private SkillResponse response = null;
        
        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="input">Skill input.</param>
        /// <param name="context">Object that allows access within the Lambda execution environment.</param>
        /// <returns>Skill output.</returns>
        public async Task<SkillResponse> FunctionHandler(SkillRequest input, ILambdaContext ctx)
        {
            context = ctx;
            try
            {
                response = new SkillResponse();
                response.Response = new ResponseBody();
                response.Response.ShouldEndSession = false;
                response.Version = "1.0";

                string locale = GetLocale(input);
                Resources resources = new Resources();
                SkillResource resource = resources.GetResources(locale);

                await SearchHandler(input, resource);

                return response;
            }
            catch (Exception ex)
            {
                Log($"error: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Handles requests while in search mode.
        /// </summary>
        /// <param name="input">User input.</param>
        /// <param name="resource">Resources.</param>
        private async Task SearchHandler(SkillRequest input, SkillResource resource)
        {
            if (input.Request is LaunchRequest)
            {
                Speak(resource.WelcomeMessage);
            }
            else if (input.Request is IntentRequest)
            {
                IntentRequest intent = (IntentRequest)input.Request;
                switch (intent.Intent.Name)
                {
                    case "AMAZON.NoIntent":
                        Speak(resource.ShutdownMessage);
                        break;
                    case "AMAZON.RepeatIntent":
                        throw new NotImplementedException();
                    case "AMAZON.HelpIntent":
                        Speak(resource.HelpMessage);
                        break;
                    case "AMAZON.StopIntent":
                    case "AMAZON.CancelIntent":
                        Speak(resource.ShutdownMessage);
                        break;
                    case "AMAZON.StartOverIntent":
                        Speak(resource.StartOverMessage);
                        break;

                    case "SearchForApplicationFee":
                        await SearchForApplicationFee(input, resource);
                        break;
                    case "SearchForTuition":
                        await SearchForTuition(input, resource);
                        break;
                    case "SearchForFinancialAid":
                        await SearchForFinancialAid(input, resource);
                        break;
                    case "SearchForAdmissionRate":
                        await SearchForAdmissionRate(input, resource);
                        break;
                }
            }
            else if (input.Request is SessionEndedRequest)
            {
                Speak(resource.ShutdownMessage);
            }
        }

        /// <summary>
        /// Construct speech response.
        /// </summary>
        /// <param name="message">Speech message.</param>
        /// <param name="shouldEndSession">Terminate session flag.</param>
        private void Speak(string message, bool shouldEndSession = true)
        {
            //if (title!= string.Empty)
            //{
            //    // Create the Simple Card content
            //    SimpleCard card = new SimpleCard();
            //    card.Title = title;
            //    card.Content = message;
            //    response.Response.Card = card;
            //}

            PlainTextOutputSpeech speechMessage = new PlainTextOutputSpeech();
            speechMessage.Text = message;

            response.Response.OutputSpeech = speechMessage;
            response.Response.ShouldEndSession = shouldEndSession;
        }

        /// <summary>
        /// Logger interface.
        /// </summary>
        /// <param name="text">Log message.</param>
        private void Log(string text)
        {
            if (context!= null)
            {
                context.Logger.LogLine(text);
            }
        }

        /// <summary>
        /// Get current locale.
        /// </summary>
        /// <param name="input">User input.</param>
        /// <returns>Current locale.</returns>
        private string GetLocale(SkillRequest input)
        {
            string locale = string.Empty;
            Dictionary<string, object> dictionary = input.Session.Attributes;
            if (dictionary!= null)
            {
                if (dictionary.ContainsKey(LOCALENAME))
                {
                    locale = (string)dictionary[LOCALENAME];
                }
            }

            if (string.IsNullOrEmpty(locale))
            {
                locale = input.Request.Locale;
            }

            if (string.IsNullOrEmpty(locale))
            {
                locale = US_LOCALE;
            }

            response.SessionAttributes = new Dictionary<string, object>()
            {
                { LOCALENAME, locale }
            };
            return locale;
        }

        /// <summary>
        /// Searches application fee by college name.
        /// </summary>
        /// <param name="input">User input.</param>
        /// <param name="resource">Resource.</param>
        private async Task SearchForApplicationFee(SkillRequest input, SkillResource resource)
        {
            string universityName = GetSlot((IntentRequest)input.Request, "universityName");
            List<SearchResult> searchResults = await SearchDatabase(universityName, "ApplicationFee");
            //response.SessionAttributes[LAST_SEARCH_KEY] = searchResults;
            //response.SessionAttributes[LAST_INTENT_KEY] = "SearchForApplicationFee";

            if (searchResults.Count > 0)
            {
                SearchResult result = searchResults[0];
                if (string.IsNullOrEmpty(result.Value))
                {
                    Speak(string.Format(resource.NoApplicationFeeFound, universityName));
                }
                else
                {
                    string message = string.Format(resource.ApplicationFeeMessage, result.UniversityName, result.Value);
                    Speak(message, false);
                }
            }
            else
            {
                Speak(string.Format(resource.NoUniversityFound, universityName));
            }
        }

        /// <summary>
        /// Searches tuition by college name.
        /// </summary>
        /// <param name="input">User input.</param>
        /// <param name="resource">Resource.</param>
        private async Task SearchForTuition(SkillRequest input, SkillResource resource)
        {
            string universityName = GetSlot((IntentRequest)input.Request, "universityName");
            List<SearchResult> searchResults = await SearchDatabase(universityName, "Tuition");

            if (searchResults.Count > 0)
            {
                SearchResult result = searchResults[0];
                if (string.IsNullOrEmpty(result.Value))
                {
                    Speak(string.Format(resource.NoTuitionFound, universityName));
                }
                else
                {
                    string message = string.Format(resource.TuitionMessage, result.UniversityName, result.Value);
                    Speak(message, false);
                }
            }
            else
            {
                Speak(string.Format(resource.NoUniversityFound, universityName));
            }
        }

        /// <summary>
        /// Searches financial aid package by college name.
        /// </summary>
        /// <param name="input">User input.</param>
        /// <param name="resource">Resource.</param>
        private async Task SearchForFinancialAid(SkillRequest input, SkillResource resource)
        {
            string universityName = GetSlot((IntentRequest)input.Request, "universityName");
            List<SearchResult> searchResults = await SearchDatabase(universityName, "FinancialAid");

            if (searchResults.Count > 0)
            {
                SearchResult result = searchResults[0];
                if (string.IsNullOrEmpty(result.Value))
                {
                    Speak(string.Format(resource.NoFinancialAidFound, universityName));
                }
                else
                {
                    string message = string.Format(resource.FinancialAidMessage, result.UniversityName, result.Value);
                    Speak(message, false);
                }
            }
            else
            {
                Speak(string.Format(resource.NoUniversityFound, universityName));
            }
        }

        /// <summary>
        /// Searches admission rate by college name.
        /// </summary>
        /// <param name="input">User input.</param>
        /// <param name="resource">Resource.</param>
        private async Task SearchForAdmissionRate(SkillRequest input, SkillResource resource)
        {
            string universityName = GetSlot((IntentRequest)input.Request, "universityName");
            List<SearchResult> searchResults = await SearchDatabase(universityName, "AdmissionRate");

            if (searchResults.Count > 0)
            {
                SearchResult result = searchResults[0];
                if (string.IsNullOrEmpty(result.Value))
                {
                    Speak(string.Format(resource.NoAdmissionRateFound, universityName));
                }
                else
                {
                    string message = string.Format(resource.AdmissionRateMessage, result.UniversityName, result.Value);
                    Speak(message, false);
                }
            }
            else
            {
                Speak(string.Format(resource.NoUniversityFound, universityName));
            }
        }

        /// <summary>
        /// Returns slot from request.
        /// </summary>
        /// <param name="request">User request.</param>
        /// <param name="slotName">Slot name.</param>
        /// <returns>Value of slot.</returns>
        private string GetSlot(IntentRequest request, string slotName)
        {
            Slot slot = request.Intent.Slots[slotName];
            if ((slot!= null) && !string.IsNullOrEmpty(slot.Value))
            {
                return slot.Value;
            }
            return null;
        }

        /// <summary>
        /// Search database for specific information.
        /// </summary>
        /// <param name="universityName">College/university name.</param>
        /// <param name="fieldName">Field to search.</param>
        /// <returns></returns>
        private async Task<List<SearchResult>> SearchDatabase(string universityName, string fieldName)
        {
            List<SearchResult> results = new List<SearchResult>();

            List<string> names = await SearchESIndex(universityName);

            using (AmazonDynamoDBClient client = new AmazonDynamoDBClient(Amazon.RegionEndpoint.USWest2))
            {
                Table universityTable = Table.LoadTable(client, TABLE_NAME);

                foreach (string name in names)
                {
                    GetItemOperationConfig config = new GetItemOperationConfig
                    {
                        AttributesToGet = new List<string> { "UniversityName", fieldName, "ImageLink" }
                    };

                    Document document = await universityTable.GetItemAsync(name, config);
                    if (document != null)
                    {
                        SearchResult result = new SearchResult()
                        {
                            UniversityName = document["UniversityName"].AsString()
                        };
                        if (document.Keys.Contains(fieldName))
                        {
                            result.Value = document[fieldName].AsString();
                        }
                        if (document.Keys.Contains("ImageLink"))
                        {
                            result.ImageLink = document["ImageLink"].AsString();
                        }

                        results.Add(result);
                        break;
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Query ElasticSearch index for possible match with university name.
        /// </summary>
        /// <param name="universityName">Raw university name.</param>
        /// <returns>List of possible matches in index.</returns>
        private async Task<List<string>> SearchESIndex(string universityName)
        {
            List<string> result = new List<string>();
            Aws4RequestSigner.AWS4RequestSigner signer = new Aws4RequestSigner.AWS4RequestSigner(ACCESS_KEY, SECRET_KEY);

            string address = ESADDRESS + INDEX_NAME + "_search";
            string message = string.Format(@"{{ ""query"": {{ ""match"": {{ ""name"": ""{0}"" }} }} }}", universityName);
            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(address),
                Content = new StringContent(message)
            };
            request = await signer.Sign(request, "es", REGION);

            string responseString = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.SendAsync(request);
                responseString = await response.Content.ReadAsStringAsync();
            }

            JObject json = JObject.Parse(responseString);
            if (json["hits"]["hits"]!= null)
            {
                JArray hits = (JArray)json["hits"]["hits"];
                foreach (JToken hit in hits)
                {
                    result.Add((string)hit["_source"]["name"]);
                }
            }

            return result;
        }
    }
}
