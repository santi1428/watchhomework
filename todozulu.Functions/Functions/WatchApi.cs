using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using homework.Common.Models;
using homework.Common.Responses;
using homework.Functions.Entities;
using System.Collections.Generic;

namespace homework.Functions.Functions
{
    public static class WatchApi
    {
        [FunctionName(nameof(CreateWorkerTime))]
        public static async Task<IActionResult> CreateWorkerTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "workertime")] HttpRequest req,
            [Table("workertime", Connection = "AzureWebJobsStorage")] CloudTable workerTimeTable,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            WorkerTime workerTime = JsonConvert.DeserializeObject<WorkerTime>(requestBody);
            WorkerTimeEntity workerTimeEntity = new WorkerTimeEntity
            {
                RowKey = Guid.NewGuid().ToString(),
                WorkerId = workerTime.WorkerId,
                DateOfReport = workerTime.DateOfReport,
                TypeOfReport = workerTime.TypeOfReport,
                Consolidated = workerTime.Consolidated,
                Timestamp = DateTime.UtcNow,
                ETag = "*",
                PartitionKey = "WORKERTIME"
            };

            TableOperation addOperation = TableOperation.Insert(workerTimeEntity);
            await workerTimeTable.ExecuteAsync(addOperation);

            string message = "New WorkerTime stored in table";

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = workerTimeEntity
            });
        }


        [FunctionName(nameof(UpdateWorkerTime))]
        public static async Task<IActionResult> UpdateWorkerTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "workertime/{workerId}")] HttpRequest req,
            [Table("workertime", Connection = "AzureWebJobsStorage")] CloudTable workerTimeTable,
            string workerId,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            WorkerTime workerTime = JsonConvert.DeserializeObject<WorkerTime>(requestBody);

            // Validate todo id
            TableOperation findOperation = TableOperation.Retrieve<WorkerTimeEntity>("WORKERTIME", workerId);
            TableResult findResult = await workerTimeTable.ExecuteAsync(findOperation);
            if (findResult.Result == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Worker time not found"
                });
            }

            // Update todo
            WorkerTimeEntity workerTimeEntity = (WorkerTimeEntity)findResult.Result;

            workerTimeEntity.DateOfReport = workerTime.DateOfReport;
            workerTimeEntity.TypeOfReport = workerTime.TypeOfReport;
            workerTimeEntity.Consolidated = workerTime.Consolidated;

            TableOperation addOperation = TableOperation.Replace(workerTimeEntity);
            await workerTimeTable.ExecuteAsync(addOperation);

            string message = $"WorkerTime: {workerId}, updated in table.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = workerTimeEntity
            });
        }

        [FunctionName(nameof(GetAllWorkerTimes))]
        public static async Task<IActionResult> GetAllWorkerTimes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "workertime")] HttpRequest req,
            [Table("workertime", Connection = "AzureWebJobsStorage")] CloudTable workerTimeTable,
            ILogger log)
        {
            log.LogInformation("Get all workertimes received.");

            TableQuery<WorkerTimeEntity> query = new TableQuery<WorkerTimeEntity>();
            TableQuerySegment<WorkerTimeEntity> workerTimes = await workerTimeTable.ExecuteQuerySegmentedAsync(query, null);

            string message = "Retrieved all worker times.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = workerTimes
            });
        }

        [FunctionName(nameof(GetWorkerTimeById))]
        public static IActionResult GetWorkerTimeById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "workertime/{workerId}")] HttpRequest req,
            [Table("workertime", "WORKERTIME", "{workerId}", Connection = "AzureWebJobsStorage")] WorkerTimeEntity workerTimeEntity,
            string workerId,
            ILogger log)
        {
            log.LogInformation($"Get todo by id: {workerId}, received.");

            if (workerTimeEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "WorkerTime not found."
                });
            }

            string message = $"Workertime: {workerTimeEntity.RowKey}, retrieved.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = workerTimeEntity
            });
        }

        [FunctionName(nameof(DeleteWorkerTime))]
        public static async Task<IActionResult> DeleteWorkerTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "workertime/{workerId}")] HttpRequest req,
            [Table("workertime", "WORKERTIME", "{workerId}", Connection = "AzureWebJobsStorage")] WorkerTimeEntity workerTimeEntity,
            [Table("workertime", Connection = "AzureWebJobsStorage")] CloudTable workerTimeTable,
            string workerId,
            ILogger log)
        {
            log.LogInformation($"Delete workertime: {workerId}, received.");

            if (workerTimeEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Workertime not found"
                });
            }

            await workerTimeTable.ExecuteAsync(TableOperation.Delete(workerTimeEntity));
            string message = $"Workertime: {workerTimeEntity.RowKey}, deleted.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = workerTimeEntity
            });
        }

        [FunctionName(nameof(InsertConsolidatedRecords))]
        public static async Task<IActionResult> InsertConsolidatedRecords(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "consolidated")] HttpRequest req,
        [Table("workertime", Connection = "AzureWebJobsStorage")] CloudTable workerTimeTable,
        [Table("consolidated", Connection = "AzureWebJobsStorage")] CloudTable consolidatedTable,
        ILogger log)
            {

            // Get records by 
            TableQuery<WorkerTimeEntity> query = new TableQuery<WorkerTimeEntity>();
            TableQuerySegment<WorkerTimeEntity> workerTimes = await workerTimeTable.ExecuteQuerySegmentedAsync(query, null);

            List<WorkerTime> workerTimeList = new List<WorkerTime>();

            foreach (WorkerTimeEntity workerTimeEntity in workerTimes)
            {
                Console.WriteLine("dateofreport = " + workerTimeEntity.DateOfReport);
                WorkerTime workerTime = new WorkerTime();
                workerTime.DateOfReport = workerTimeEntity.DateOfReport;
                workerTime.WorkerId = workerTimeEntity.WorkerId;
                workerTime.TypeOfReport = workerTimeEntity.TypeOfReport;
                workerTime.Consolidated = workerTimeEntity.Consolidated;
                workerTimeList.Add(workerTime);
            }

            workerTimeList.Sort((a, b) => a.DateOfReport.CompareTo(b.DateOfReport));
            workerTimeList.Sort((a, b) => a.WorkerId.CompareTo(b.WorkerId));

            List<Consolidated> consolidatedList = new List<Consolidated>();

            double minutesOfWork = 0;

            for (int i = 0; i < workerTimeList.Count; i++)
            {
                if (workerTimeList[i].TypeOfReport == 0 && workerTimeList[i + 1].TypeOfReport == 1)
                {
                    if (workerTimeList[i].WorkerId == workerTimeList[i + 1].WorkerId)
                    {
                        minutesOfWork = minutesOfWork + getMinutesOfWork(workerTimeList[i].DateOfReport, workerTimeList[i + 1].DateOfReport);
                    }
                    else
                    {
                        ConsolidatedEntity consolidatedEntity = new ConsolidatedEntity
                        {
                            RowKey = Guid.NewGuid().ToString(),
                            WorkerId = workerTimeList[i].WorkerId,
                            DateOfReport = workerTimeList[i].DateOfReport,
                            MinutesOfWork = minutesOfWork,
                            Timestamp = DateTime.UtcNow,
                            ETag = "*",
                            PartitionKey = "CONSOLIDATED"
                        };
                        TableOperation addOperation = TableOperation.Insert(consolidatedEntity);
                        await consolidatedTable.ExecuteAsync(addOperation);
                        minutesOfWork = 0;
                    }
                }
            }

            return new OkObjectResult(new Response
            {
                    IsSuccess = true,
                    Message = "prueba",
                    Result = workerTimeList
            });
        }


        public static double getMinutesOfWork(DateTime date1, DateTime date2)
        {
            TimeSpan ts = date2 - date1;
            return ts.TotalMinutes;

        }
    }
}
