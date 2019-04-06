using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Tutorial4
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
        public async Task OrchestratorFunction([OrchestrationTrigger]DurableOrchestrationContextBase durable)
        {
            var input = durable.GetInput<int>();

            for (int i = 1; i <= 5; i++)
            {
                try
                {
                    await durable.CallActivityAsync(nameof(ActivityFunctions.ActivityAcceptZero), input);
                    return;
                }
                catch (FunctionFailedException e) when (e.InnerException is ArgumentException)
                {
                    var nextWakeUp = durable.CurrentUtcDateTime.AddSeconds(i * 5);
                    var timer = durable.CreateTimer(nextWakeUp, CancellationToken.None);
                    await timer;
                    input = input - 1;
                }
            }

            throw new Exception("failed this orchestrator");
        }
    }

    public class ActivityFunctions
    {
        [FunctionName(nameof(ActivityAcceptZero))]
        public Task ActivityAcceptZero([ActivityTrigger] int input)
        {
            if (input != 0)
            {
                throw new ArgumentException("input must be zero");
            }

            return Task.FromResult(0);
        }

        [FunctionName(nameof(ActivityDelay))]
        public async Task<bool> ActivityDelay([ActivityTrigger] DurableActivityContextBase durable)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            return true;
        }
    }
}
