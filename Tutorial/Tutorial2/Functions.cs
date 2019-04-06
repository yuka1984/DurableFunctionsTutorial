using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Tutorial2
{
    public class OrchestratorFunctions
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

        [FunctionName(nameof(OrchestratorFunction))]
        public async Task<int> OrchestratorFunction([OrchestrationTrigger]DurableOrchestrationContextBase durable)
        {
            var input = durable.GetInput<int>();
            while (true)
            {
                try
                {
                    input = await durable.CallActivityAsync<int>(nameof(ActivityFunctions.ActivityNumber2x), input);
                    break;
                }
                catch (FunctionFailedException ex) when (ex.InnerException is ArgumentException)
                {
                    input = await durable.CallActivityAsync<int>(nameof(ActivityFunctions.ActivityNumber1Minus), input);
                }
            }

            try
            {
                input = await durable.CallActivityAsync<int>(nameof(ActivityFunctions.ActivityNumber5x), input);
            }
            catch
            {
                // ignored
            }
            finally
            {
                input = await durable.CallActivityAsync<int>(nameof(ActivityFunctions.ActivityNumber10x), input);
            }

            return input;
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

        [FunctionName(nameof(ActivityNumber1Minus))]
        public Task<int> ActivityNumber1Minus([ActivityTrigger] int input)
        {            
            return Task.FromResult(input - 1);
        }

        [FunctionName(nameof(ActivityNumber5x))]
        public Task<int> ActivityNumber5x([ActivityTrigger] int input)
        {
            throw new Exception("error");
        }

        [FunctionName(nameof(ActivityNumber10x))]
        public Task<int> ActivityNumber10x([ActivityTrigger] int input)
        {
            return Task.FromResult(input * 10);
        }
    }
}
