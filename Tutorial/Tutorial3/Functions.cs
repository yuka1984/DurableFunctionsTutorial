using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Tutorial3
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
    }

    public class OrchestratorFunctions
    {
        [FunctionName(nameof(OrchestratorFunction))]
        public async Task<int> OrchestratorFunction([OrchestrationTrigger]DurableOrchestrationContextBase durable)
        {
            var input = durable.GetInput<int>();

            var number2xTasks = Enumerable
                .Range(1, input)
                .Select(x => durable.CallActivityAsync<int>(nameof(ActivityFunctions.ActivityNumber2x), x));

            var results = await Task.WhenAll(number2xTasks);
            var result = results.Sum();
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
