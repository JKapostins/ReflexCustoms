using Amazon.Lambda.Core;
using ReflexUtility;
using System;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace PollReflexCentralForNewTracks
{
    public class Function
    {

        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public void FunctionHandler(Amazon.Lambda.SQSEvents.SQSEvent sqsEvent, ILambdaContext context)
        {
            PollTracks.Poll(sqsEvent, context, false);
        }

    }
}
