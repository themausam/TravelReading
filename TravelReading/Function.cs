using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET.Request;
using Alexa.NET.Response;
using Amazon.Lambda.Core;
using Alexa.NET.Request.Type;
using System.Net.Http;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace TravelReading
{
    public class Function
    {
        private static HttpClient _httpClient;

        public const string INVOCATION_NAME = "Reading Travel";
        public const string TRAVEL_API_KEY = "e29bef24b01ad7046ce0e738109242ab";
        public const string TRAVEL_API_ID = "e7f6be84";

        public Function()
        {
            _httpClient = new HttpClient();
        }

        public async Task<SkillResponse> FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            var requestType = input.GetRequestType();
            if (requestType == typeof(IntentRequest))
            {
                var intentRequest = input.Request as IntentRequest;
                var postcodeRequested = intentRequest.Intent.Slots["postcode"].Value;

                if (string.IsNullOrEmpty(postcodeRequested))
                    return MakeSkillResponse("I am sorry I didn't understand the postcode you are trying to search. Please ask again.", false);

                var postcodeInfo = await GetCoOrdinatesForPostCode(postcodeRequested, context);

                var busStops = await GetStopsForCoOrdinates(postcodeInfo, context);

                string stopsInfo = null;

                stopsInfo = string.Join(",", busStops.Select(b => b.stop_name + " bearing " + b.bearing).ToList());

                return MakeSkillResponse($"There are { busStops.Count().ToString() } stops near { postcodeRequested }. They are { stopsInfo }.", true);
            }
            else
            {
                return MakeSkillResponse($"I don't know how to handle this intent. Please say something like Alexa, ask { INVOCATION_NAME } for nearby bus stops", true);
            }
        }

        private SkillResponse MakeSkillResponse(string outputSpeech, bool shouldEndSession, string repromptText = "Reprompting. To exit, say, exit.")
        {
            var response = new ResponseBody
            {
                ShouldEndSession = shouldEndSession,
                OutputSpeech = new PlainTextOutputSpeech { Text = outputSpeech }
            };

            if (repromptText != null)
            {
                response.Reprompt = new Reprompt() { OutputSpeech = new PlainTextOutputSpeech() { Text = repromptText } };
            }

            var skillReponse = new SkillResponse
            {
                Response = response,
                Version = "1.0"
            };

            return skillReponse;
        }

        private async Task<PostCode> GetCoOrdinatesForPostCode(string postcode, ILambdaContext context)
        {
            var uri = new Uri($"https://api.postcodes.io/postcodes/{postcode}");
            var postCode = new PostCode();
            context.Logger.LogLine($"Attempting to fetch data from {uri.AbsoluteUri}");
            try
            {
                var response = await _httpClient.GetStringAsync(uri);
                context.Logger.LogLine($"Response from URL:\n{response}");
                // TODO: (PMO) Handle bad requests
                var postcodeResult = JsonConvert.DeserializeObject<PostCodeResult>(response);
                return postcodeResult.result;
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"\nException: {ex.Message}");
                context.Logger.LogLine($"\nStack Trace: {ex.StackTrace}");
            }
            return null;
        }

        private async Task<List<BusStop>> GetStopsForCoOrdinates(PostCode postcode, ILambdaContext context)
        {
            var uri = new Uri($"https://transportapi.com/v3/uk/bus/stops/near.json?app_id={TRAVEL_API_ID}&app_key={TRAVEL_API_KEY}&lat={postcode.latitude}&lon={postcode.longitude}&page=1&rpp=5");
            var postCode = new PostCode();
            context.Logger.LogLine($"Attempting to fetch data from {uri.AbsoluteUri}");
            try
            {
                var response = await _httpClient.GetStringAsync(uri);
                context.Logger.LogLine($"Response from URL:\n{response}");
                // TODO: (PMO) Handle bad requests
                var busStopsResult = JsonConvert.DeserializeObject<BusStopsResult>(response);
                return busStopsResult.stops;
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"\nException: {ex.Message}");
                context.Logger.LogLine($"\nStack Trace: {ex.StackTrace}");
            }
            return null;
        }

        private class BusStopsResult
        {
            public List<BusStop> stops { get; set; }
        }

        private class BusStop
        {
            public string stop_name { get; set; }
            public string bearing { get; set; }
        }

        private class PostCodeResult
        {
            public int status { get; set; }
            public PostCode result { get; set; }
        }

        private class PostCode
        {
            public string longitude { get; set; }
            public string latitude { get; set; }
        }
    }
}