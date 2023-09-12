using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using daniel_bot.Model;
using RestSharp;
using System.Threading.Tasks;
using System.Linq;
using System.Text;

namespace daniel_bot.DataService
{
    public class BotDataService
    {
        public IWebHostEnvironment WebHostEnvioronment { get; set; }

        public BotDataService(IWebHostEnvironment webhostEnviron) => WebHostEnvioronment = webhostEnviron;

        public async Task<string> LookUpPlateInfoAsync(string platenumber)
        {
            StringBuilder sb = new StringBuilder();
            var options = new RestClientOptions(@"https://data.cityofnewyork.us")
            {
                MaxTimeout = -1,
            };
            var client = new RestClient(options);
            var request = new RestRequest($"/resource/nc67-uf89.json?plate={platenumber}", Method.Get);
            RestResponse response = await client.ExecuteAsync(request);
            PlateViolation vio = JsonSerializer.Deserialize<IEnumerable<PlateViolation>>(response.Content).FirstOrDefault();

            if (vio != null)
            {
                sb.AppendLine($"Ok! so here is what I found for license plate {vio.PlateId}:")
                    .AppendLine($"Violation: {vio.Violation}")
                    .AppendLine($"Date Issued: {vio.IssueDate}")
                    .AppendLine($"Fine Ammount: {vio.FineAmount}")
                    .AppendLine("You can vist https://a836-citypay.nyc.gov/ to pay this ticket.");
            }
            else
                sb.AppendLine($"I could not find anything for license plate {platenumber}.");
            return sb.ToString();
        }
    }
}