using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET.Request;
using Alexa.NET.Response;
using Amazon.Lambda.Core;
using Alexa.NET.Request.Type;
using System.Net.Http;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace TravelReading
{
    public class Function
    {
        private static HttpClient _httpClient;

        public const string INVOCATION_NAME = "Reading Travel";

        public Function()
        {
            _httpClient = new HttpClient();
        }

        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext context)
        {
            var requestType = input.GetRequestType();
            if (requestType == typeof(IntentRequest))
            {
                var intentRequest = input.Request as IntentRequest;
                var postcodeRequested = intentRequest.Intent.Slots["postcode"].Value;
                return MakeSkillResponse($"You want to know about { postcodeRequested }. I will look that up for you.", true);
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
    }
}