using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DurableTask.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Tutorial9
{
    public class TimerFunctions
    {
        [FunctionName(nameof(Purge))]
        public async Task Purge([TimerTrigger("0 0 20 * * *")]TimerInfo timerInfo, [OrchestrationClient]DurableOrchestrationClient durableClient)
        {
            await durableClient.PurgeInstanceHistoryAsync(
                DateTime.MinValue,
                DateTime.UtcNow.AddMonths(-1),
                new[] {OrchestrationStatus.Completed});
        }
    }
}
