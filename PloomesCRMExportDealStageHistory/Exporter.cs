using Microsoft.Extensions.Hosting;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Data.Common;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;

namespace PloomesCRMExportDealStageHistory
{
    public class Exporter : BackgroundService
    {
        private readonly PloomesService _ploomesService;
        private readonly IHostApplicationLifetime _applicationLifetime;

        public Exporter(PloomesService ploomesService, IHostApplicationLifetime hostApplicationLifetime) 
        { 
            _ploomesService= ploomesService;
            _applicationLifetime = hostApplicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.Clear();
            Console.WriteLine("De quantos dias atrás (a partir de hoje) você gostaria de extrair?");
            int days = Convert.ToInt32(Console.ReadLine());

            string subPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/Exportacoes/";
            bool exists = Directory.Exists(subPath);
            if (!exists)
                Directory.CreateDirectory(subPath);

            string path = subPath + DateTime.Now.Date.ToString("yyyy-MM-dd") + "-" + Guid.NewGuid().ToString() + ".xlsx";

            Console.WriteLine("Estamos iniciando a exportação...");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using ExcelPackage package = new();
            var workSheet = package.Workbook.Worksheets.Add("Historico de Chamados");

            workSheet.Row(1).Style.Font.Bold = true;

            workSheet.Cells[1, 1].Value = "ID do Negócio";
            workSheet.Cells[1, 2].Value = "Título";
            workSheet.Cells[1, 3].Value = "Data de Criação";
            workSheet.Cells[1, 4].Value = "Data de Finalização";

            int columnNumber = 5;
            JsonArray stages = await _ploomesService.GetStages();
            foreach (JsonNode stage in stages)
            {
                workSheet.Cells[1, columnNumber].Value = stage["Name"];

                stage["Column"] = columnNumber;
                columnNumber++;
            }

            int totalColumns = columnNumber;

            workSheet.Cells[1, totalColumns].Value = "Tempo total";

            int lineNumber = 2;
            JsonArray deals = await _ploomesService.GetDeals(days);
            foreach (JsonNode deal in deals)
            {
                workSheet.Cells[lineNumber, 1].Value = deal["Id"];
                workSheet.Cells[lineNumber, 2].Value = deal["Name"];
                workSheet.Cells[lineNumber, 3].Value = Convert.ToDateTime(deal["CreateDate"].ToString());
                workSheet.Cells[lineNumber, 3].Style.Numberformat.Format = "dd/MM/yyyy HH:mm:ss";
                workSheet.Cells[lineNumber, 4].Value = Convert.ToDateTime(deal["FinishDate"]?.ToString());
                workSheet.Cells[lineNumber, 4].Style.Numberformat.Format = "dd/MM/yyyy HH:mm:ss";

                double totalHours = 0;
                int colWithMaxTime = 0;
                TimeSpan maxTime = TimeSpan.FromSeconds(0);
                foreach (JsonNode dealStage in deal["Stages"].AsArray())
                {
                    if (Convert.ToInt32(dealStage["StageId"].ToString()) is 1 or 2 or 3)
                        continue;
                    
                    columnNumber = (int)stages.Where(s => s["Id"].ToString() == dealStage["StageId"].ToString()).FirstOrDefault()["Column"];

                    TimeSpan ts = TimeSpan.FromSeconds(Convert.ToDouble(dealStage["Seconds"].ToString(), new CultureInfo("en-US")));

                    double timeFormated = ts.TotalDays;
                    totalHours += timeFormated;

                    workSheet.Cells[lineNumber, columnNumber].Value = timeFormated;
                    workSheet.Cells[lineNumber, columnNumber].Style.Numberformat.Format = "HH:mm:ss";

                    if (ts > maxTime) 
                    {
                        maxTime = ts;
                        colWithMaxTime = columnNumber;
                    }
                }


                workSheet.Cells[lineNumber, totalColumns].Value = totalHours;
                workSheet.Cells[lineNumber, totalColumns].Style.Numberformat.Format = "HH:mm:ss";

                Color colFromHex = ColorTranslator.FromHtml("#FFFF00");
                workSheet.Cells[lineNumber, colWithMaxTime].Style.Fill.PatternType = ExcelFillStyle.Solid;
                workSheet.Cells[lineNumber, colWithMaxTime].Style.Fill.BackgroundColor.SetColor(colFromHex);

                lineNumber++;
            }

            workSheet.Columns.AutoFit();

            FileStream objFileStrm = File.Create(path);
            objFileStrm.Close();

            File.WriteAllBytes(path, package.GetAsByteArray());

            Console.WriteLine("Planilha exportada com sucesso :)");
            Console.WriteLine("Caminho: " + path);

            _applicationLifetime.StopApplication();
        }
    }
}
