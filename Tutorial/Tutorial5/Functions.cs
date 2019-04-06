using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Tutorial5
{
    public class StarterFunctions
    {
        [FunctionName(nameof(Starter))]
        public async Task<HttpResponseMessage> Starter(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "starter/{input}")]HttpRequestMessage requestMessage,
            int input,
            [OrchestrationClient]DurableOrchestrationClient durableClient)
        {
            var instanceId = await durableClient.StartNewAsync(nameof(OrchestratorFunctions.OrchestratorFunction), input);
            return durableClient.CreateCheckStatusResponse(requestMessage, instanceId);
        }

        [FunctionName(nameof(External))]
        public async Task<HttpResponseMessage> External(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "external/{instanceId}/{taskHubName}/{input}")]HttpRequestMessage requestMessage,
            string instanceId,
            string taskHubName,
            int input,
            [OrchestrationClient]DurableOrchestrationClient durableClient)
        {
            await durableClient.RaiseEventAsync(taskHubName, instanceId, "input_number", input);
            return requestMessage.CreateResponse();
        }
    }

    public class OrchestratorFunctions
    {
        [FunctionName(nameof(OrchestratorFunction))]
        public async Task<int> OrchestratorFunction([OrchestrationTrigger]DurableOrchestrationContextBase durable)
        {
            var input = durable.GetInput<int>();
            input = await durable.CallActivityAsync<int>(nameof(ActivityFunctions.ActivityNumber2x), input);
            var externalInput = await durable.WaitForExternalEvent<int>("input_number", TimeSpan.FromMinutes(10));
            return input + externalInput;
        }
    }

    public class ActivityFunctions
    {
        [FunctionName(nameof(ActivityNumber2x))]
        public Task<int> ActivityNumber2x([ActivityTrigger] int input)
        {
            return Task.FromResult(input * 2);
        }
    }
}
