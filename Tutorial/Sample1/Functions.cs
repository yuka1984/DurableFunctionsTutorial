using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;

namespace Sample1
{
    public class StarterFunctions
    {
        public const string InstanceId = "SampleFunctionsInstanceId";

        [FunctionName(nameof(Starter))]
        public async Task Starter(
            [TimerTrigger("*/10 * * * * *")] TimerInfo timerInfo,
            [Table("LastRun")]CloudTable lastRunTable,
            Binder binder,
            [OrchestrationClient]DurableOrchestrationClient durableClient,            
            ILogger logger)
        {
            var query = new TableQuery<LastRunTableEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, InstanceId));
            var token = default(TableContinuationToken);
            var tableResulet= await lastRunTable.ExecuteQuerySegmentedAsync(query, token);

            if (tableResulet.Results.Any())
            {
                var lastRun = tableResulet.Results.OrderByDescending(x => x.Timestamp).First();
                DurableOrchestrationClientBase taskDurable = durableClient;
                if (durableClient.TaskHubName == lastRun.TaskHubName)
                {
                    taskDurable = await binder.BindAsync<DurableOrchestrationClientBase>(new OrchestrationClientAttribute { TaskHub = lastRun.TaskHubName });
                }
                var lastRunStatus = await taskDurable.GetStatusAsync(InstanceId);
                if (lastRunStatus != null && 
                    (lastRunStatus.RuntimeStatus == OrchestrationRuntimeStatus.Running || lastRunStatus.RuntimeStatus == OrchestrationRuntimeStatus.Pending))
                {
                    logger.LogWarning("Last run orchestrator is still running");
                    return;
                }
            }

            var instanceId =
                await durableClient.StartNewAsync(nameof(OrchestratorFunctions.OrchestratorFunction), InstanceId, null);

            await lastRunTable.ExecuteAsync(TableOperation.InsertOrReplace(new LastRunTableEntity
            {
                PartitionKey = InstanceId,
                RowKey = "",
                TaskHubName = durableClient.TaskHubName
            }));
        }

        public class LastRunTableEntity : TableEntity
        {
            public string TaskHubName { get; set; }
        }
    }

    public class OrchestratorFunctions
    {
        [FunctionName(nameof(OrchestratorFunction))]
        public async Task<int> OrchestratorFunction([OrchestrationTrigger]DurableOrchestrationContextBase durable)
        {
            var input = durable.GetInput<int>();

            var result = await durable.CallActivityAsync<int>(nameof(ActivityFunctions.ActivityNumber2x), input);

            return result;
        }
    }

    public class ActivityFunctions
    {
        [FunctionName(nameof(ActivityNumber2x))]
        public async Task<int> ActivityNumber2x([ActivityTrigger] int input)
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            return input * 2;
        }

        [FunctionName(nameof(ActivityNumber10x))]
        public Task<int> ActivityNumber10x([ActivityTrigger] int input)
        {
            return Task.FromResult(input * 10);
        }
    }
}
