using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Tutorial3
{
    public class SampleOrchestratorFunctions
    {
        [FunctionName(nameof(SampleOrchestratorFunction))]
        public async Task<int> SampleOrchestratorFunction([OrchestrationTrigger]DurableOrchestrationContextBase durable)
        {
            var input = durable.GetInput<int>();
            var twoXTask = durable.CallActivityAsync<int>(nameof(ActivityFunctions.ActivityNumber2x), input);
            var tenXTask = durable.CallActivityAsync<int>(nameof(ActivityFunctions.ActivityNumber10x), input);
            var results = await Task.WhenAll(new[] {twoXTask, tenXTask});
            return results.Sum();
        }
    }
}
