using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Tutorial6
{
    public class StarterFunctions
    {
        [FunctionName(nameof(StarterAsync))]
        public async Task<HttpResponseMessage> StarterAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "starter/async")]HttpRequestMessage requestMessage,
            [OrchestrationClient]DurableOrchestrationClient durableClient)
        {
            var instanceId = await durableClient.StartNewAsync(nameof(OrchestratorFunctions.OrchestratorFunction), 10);
            return durableClient.CreateCheckStatusResponse(requestMessage, instanceId);
        }

        [FunctionName(nameof(StarterSync))]
        public async Task<HttpResponseMessage> StarterSync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "starter/sync")]HttpRequestMessage requestMessage,
            [OrchestrationClient]DurableOrchestrationClient durableClient)
        {
            var instanceId = await durableClient.StartNewAsync(nameof(OrchestratorFunctions.OrchestratorFunction), 10);
            return await durableClient.WaitForCompletionOrCreateCheckStatusResponseAsync(requestMessage, instanceId, TimeSpan.FromSeconds(10));
        }

        [FunctionName(nameof(StarterSpecify))]
        public async Task<HttpResponseMessage> StarterSpecify(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "starter/instanceId/{instanceId}")]HttpRequestMessage requestMessage,
            string instanceId,
            [OrchestrationClient]DurableOrchestrationClient durableClient)
        {
            var status = await durableClient.GetStatusAsync(instanceId);
            if (status != null)
            {
                return durableClient.CreateCheckStatusResponse(requestMessage, instanceId);
            }
            await durableClient.StartNewAsync(nameof(OrchestratorFunctions.OrchestratorFunction), instanceId, 10);
            return durableClient.CreateCheckStatusResponse(requestMessage, instanceId);
        }
    }

    public class OrchestratorFunctions
    {
        [FunctionName(nameof(OrchestratorFunction))]
        public async Task<int> OrchestratorFunction([OrchestrationTrigger]DurableOrchestrationContextBase durable)
        {
            var input = durable.GetInput<int>();
            var number = await durable.CallActivityAsync<int>(nameof(ActivityFunctions.ActivityNumber2x), input);
            var result = await durable.CallActivityAsync<int>(nameof(ActivityFunctions.ActivityNumber10x), number);
            return result;
        }
    }

    public class ActivityFunctions
    {
        [FunctionName(nameof(ActivityNumber2x))]
        public Task<int> ActivityNumber2x([ActivityTrigger] int input)
        {
            return Task.FromResult(input * 2);
        }

        [FunctionName(nameof(ActivityNumber10x))]
        public Task<int> ActivityNumber10x([ActivityTrigger] int input)
        {
            return Task.FromResult(input * 10);
        }
    }
}
