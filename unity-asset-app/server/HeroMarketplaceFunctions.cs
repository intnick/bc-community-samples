using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.WindowsAzure.Storage.Table;
using System.Net;

namespace HeroMarketplace
{
    public static class HeroMarketplaceFunctions
    {
        [FunctionName("Test")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            return name != null
                ? (ActionResult)new OkObjectResult($"Hello, {name}")
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }

        [FunctionName("Offers")]
        public static async Task<HttpResponseMessage> GetOffers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
            HttpRequest req,
            [Table("Offers")] CloudTable cloudTable,
            ILogger log)
        {
            log.LogInformation("GetOffers HTTP trigger function");

            TableQuery<OfferEntity> query = new TableQuery<OfferEntity>();

            var offerEntities = await cloudTable.ExecuteQuerySegmentedAsync(query, null);
            var offerList = offerEntities.Select(e => new Offer(e));
            var result = JsonConvert.SerializeObject(offerList);
            return new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StringContent(result) };
        }

        [FunctionName("UserOffers")]
        public static async Task<HttpResponseMessage> GetUserOffers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Table("Offers")] CloudTable cloudTable,
            ILogger log)
        {
            log.LogInformation("GetOffers HTTP trigger function");

            var user = req.Query["user"];
            TableQuery<OfferEntity> query = new TableQuery<OfferEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, user)); ;

            var offerEntities = await cloudTable.ExecuteQuerySegmentedAsync(query, null);
            var offerList = offerEntities.Select(e => new Offer(e));
            var result = JsonConvert.SerializeObject(offerList);
            return new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StringContent(result) };
        }

        public class OfferEntity : TableEntity
        {
            public string Price { get; set; }
        }

        public class Offer
        {
            public string hero { get; set; }

            public double price { get; set; }

            public string seller { get; set; }

            public Offer(OfferEntity entity)
            {
                hero = entity.RowKey;
                seller = entity.PartitionKey;
                price = Convert.ToDouble(entity.Price);
            }
        }
    }
}
