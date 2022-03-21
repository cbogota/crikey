using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Web;
using System.Net.Http.Json;

namespace UtilityFunctions
{
    public static class Function1
    {
        public class MadiResult
        {
            public bool statusOk { get; set; }
            public string exception { get; set; }
            public bool result { get; set; }
            public string retMessage { get; set; }
        }
        public static HttpClient MadiClient { get; } = new() { BaseAddress = new Uri("https://account.alberta.ca/") };
        public static string MadiEmailQuery(string email) => $"sa/doesemailexist?mail={email}";
        public static HttpClient MadiBClient { get; } = new() { BaseAddress = new Uri("https://business.account.alberta.ca/") };
        public static string MadiBEmailQuery(string email) => $"sawrapper/doesemailexist?mail={email}&loginType=business";

        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string email = req.Query["email"];
            string emailEncoded = HttpUtility.UrlEncode(email ?? "");

            MadiResult madiResult = new MadiResult();
            MadiResult madiBResult = new MadiResult();
            if (!string.IsNullOrEmpty(emailEncoded))
            {
                try
                {
                    var madiResponse = await MadiClient.GetAsync(MadiEmailQuery(emailEncoded));
                    madiResult = await madiResponse.Content.ReadFromJsonAsync<MadiResult>();
                    madiResult.statusOk = madiResponse.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    madiResult = new MadiResult { exception = ex.Message };
                }
                try
                {
                    var madiBResponse = await MadiBClient.GetAsync(MadiBEmailQuery(emailEncoded));
                    madiBResult = await madiBResponse.Content.ReadFromJsonAsync<MadiResult>() ?? new MadiResult();
                    madiBResult.statusOk = madiBResponse.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    madiBResult = new MadiResult { exception = ex.Message };
                }
            }

            return new OkObjectResult(new { email, madiResult, madiBResult });
        }
    }
}
