using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PloomesCRMExportDealStageHistory
{
    public class PloomesService
    {
        private readonly RequestService _requestService;
        private readonly string _url;
        private readonly Dictionary<string, string> _headers;

        public PloomesService(RequestService requestService, IConfiguration configuration) 
        { 
            _requestService = requestService;
            _url = configuration.GetValue<string>("API2Url");
            _headers = new Dictionary<string, string>() { { "User-Key", configuration.GetValue<string>("UserKey") } };
        }

        public async Task<JsonArray> GetStages()
        {
            string url = _url + "Deals@Stages?$filter=PipelineId+eq+35992&$orderby=Ordination";

            return JsonSerializer.Deserialize<JsonObject>(await _requestService.Request(url, HttpMethod.Get, _headers))["value"].AsArray();
        }

        public async Task<JsonArray> GetDeals(int days)
        {
            string filterDate = DateTime.Now.AddDays(days * -1).ToString("yyyy-MM-dd");

            int top = 100;
            int skip = 0;

            
            JsonArray dealHistoryResponse = new ();

            string url = _url + $"Deals@Stages@History?$select=CreateDate,UpdateDate,StageId,DealId&$expand=Deal($select=Title,CreateDate,FinishDate)&$top={top}&$skip={skip}&$filter=Deal/PipelineId+eq+35992+and+Stage/PipelineId+eq+35992+and+Deal/CreateDate+ge+{filterDate}";
            dealHistoryResponse = JsonSerializer.Deserialize<JsonObject>(await _requestService.Request(url, HttpMethod.Get, _headers))["value"].AsArray();
            IEnumerable<object> dealStageHistories = dealHistoryResponse.ToList<object>();

            while (dealHistoryResponse.Count > 0)
            {
                skip += 100;

                url = _url + $"Deals@Stages@History?$select=CreateDate,UpdateDate,StageId,DealId&$expand=Deal($select=Title,CreateDate)&$top={top}&$skip={skip}&$filter=Deal/PipelineId+eq+35992+and+Stage/PipelineId+eq+35992+and+Deal/CreateDate+ge+{filterDate}";
                dealHistoryResponse = JsonSerializer.Deserialize<JsonObject>(await _requestService.Request(url, HttpMethod.Get, _headers))["value"].AsArray();
                dealStageHistories = dealStageHistories.Concat(dealHistoryResponse.ToList<object>());

                Task.Delay(1000);
            }

            JsonArray response = new();

            foreach(JsonNode dealStageHistory in dealStageHistories)
            {
                JsonObject newDealStageHistory = (JsonObject)response.Where(dh => dh["Id"].ToString() == dealStageHistory["DealId"].ToString()).FirstOrDefault();

                double seconds = (DateTime.Parse(dealStageHistory["UpdateDate"]?.ToString() ?? DateTime.Now.ToString()) - DateTime.Parse(dealStageHistory["CreateDate"].ToString())).TotalSeconds;

                if (newDealStageHistory == null)
                {
                    JsonArray stages = new()
                    {
                        new JsonObject() { { "StageId", dealStageHistory["StageId"].ToString() }, { "Seconds", seconds } }
                    };

                    newDealStageHistory = new()
                    {
                        { "Id", dealStageHistory["DealId"].ToString() },
                        { "Name", dealStageHistory["Deal"]["Title"].ToString() },
                        { "CreateDate", dealStageHistory["Deal"]["CreateDate"].ToString() },
                        { "FinishDate", dealStageHistory["Deal"]["FinishDate"]?.ToString() },
                        { "Stages", stages }
                    };

                    response.Add(newDealStageHistory);
                }
                else
                {
                    JsonArray stages = newDealStageHistory["Stages"].AsArray();

                    JsonObject newStage = (JsonObject)stages.Where(s => s["StageId"].ToString() == dealStageHistory["StageId"].ToString()).FirstOrDefault();

                    if(newStage == null)
                    {
                        newStage = new JsonObject() { { "StageId", dealStageHistory["StageId"].ToString() }, { "Seconds", seconds } };

                        stages.Add(newStage);
                    }
                    else
                    {
                        seconds += Convert.ToDouble(newStage["Seconds"].ToString(), new CultureInfo("en-US"));

                        newStage["Seconds"] = seconds;
                    }
                }
            }

            return response;
        }
    }
}
