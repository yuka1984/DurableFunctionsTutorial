using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Tutorial7
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
            var retryOption = new RetryOptions(TimeSpan.FromSeconds(5), 3)
            {
                MaxRetryInterval = TimeSpan.FromSeconds(30),
                Handle = exception =>
                {
                    return exception.InnerException is ArgumentException;
                },
            };
            var number =
                await durable.CallActivityWithRetryAsync<int>(nameof(ActivityFunctions.ActivityNumber2x),
                    retryOption,
                    input);
            return number;
        }
    }

    public class ActivityFunctions
    {
        [FunctionName(nameof(ActivityNumber2x))]
        public Task<int> ActivityNumber2x([ActivityTrigger] int input)
        {
            if (input % 2 == 0)
            {
                throw new ArgumentException("input failed");
            }

            return Task.FromResult(input * 2);
        }
    }
}
