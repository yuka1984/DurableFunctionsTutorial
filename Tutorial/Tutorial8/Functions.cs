using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Tutorial8
{
    public class StarterFunctions
    {
        [FunctionName(nameof(Starter))]
        public async Task<HttpResponseMessage> Starter(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "starter")]HttpRequestMessage requestMessage,
            [OrchestrationClient]DurableOrchestrationClient durableClient)
        {
            var json = await requestMessage.Content.ReadAsStringAsync();
            var input = JsonConvert.DeserializeObject<int[]>(json);
            var instanceId = await durableClient.StartNewAsync(nameof(OrchestratorFunctions.MultiOrchestratorFunction), input);
            return await durableClient.WaitForCompletionOrCreateCheckStatusResponseAsync(requestMessage, instanceId);
        }
    }

    public class OrchestratorFunctions
    {
        [FunctionName(nameof(MultiOrchestratorFunction))]
        public async Task<int[]> MultiOrchestratorFunction([OrchestrationTrigger]DurableOrchestrationContextBase durable)
        {
            var input = durable.GetInput<int[]>();
            var subOrchestrators =
                input.Select(x => durable.CallSubOrchestratorAsync<int>(nameof(OrchestratorFunction), x))
                    .ToArray();
            var result = await Task.WhenAll(subOrchestrators);
            return result;
        }

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
