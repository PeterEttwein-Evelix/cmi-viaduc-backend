using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Order;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.Auth;
using CMI.Web.Management.Helpers;
using MassTransit;
using Newtonsoft.Json;
using Serilog;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    public class ReportController : ApiManagementControllerBase
    {
        private readonly ExcelExportHelper exportHelper;
        private readonly IReportExternalContentHelper externalContentClient;
        private readonly IPublicOrder orderManagerClient;

        public ReportController(IPublicOrder client, ExcelExportHelper exportHelper, IReportExternalContentHelper externalContentClient)
        {
            this.orderManagerClient = client;
            this.exportHelper = exportHelper;
            this.externalContentClient = externalContentClient;
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetPrimaerdatenRecords(DateTime startDate, DateTime endDate)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.ReportingStatisticsReportsEinsehen);
            try
            {
                var rawResult =
                    await this.orderManagerClient.GetPrimaerdatenReportRecords(new LogDataFilter {StartDate = startDate, EndDate = endDate});
                var mutationIds = rawResult.Where(r => r.MutationsId.HasValue)
                    .Select(r => r.MutationsId.Value).ToArray();
                List<SyncInfoForReport> externalContent = null;
                if (mutationIds.Length > 0)
                {
                    externalContent = await externalContentClient.GetSyncInfoForReport(mutationIds);
                }

                var response = ConvertToPrimaerdatenRecord(rawResult, externalContent);

                var retVal = CreateExcelFile(response);

                return ResponseMessage(retVal);
            }
            catch (RequestTimeoutException ex)
            {
                Log.Error(ex, "Timeout while fetching the records");
                return BadRequest(
                    "Timeout beim Holen der Daten-Einträge. Der gewählte Zeitraum umfasst vermutlich zu viele Daten-Einträge. Bitte wählen Sie einen kürzeren Zeitraum");
            }
            catch (RequestFaultException ex)
            {
                Log.Error(ex, "Rabbit Mq error while fetching the log records");
                return BadRequest(
                    "Fehler beim Holen der Daten-Einträge. Der gewählte Zeitraum umfasst vermutlich zu viele Daten-Einträge. Bitte wählen Sie einen kürzeren Zeitraum");
            }
            catch (OutOfMemoryException ex)
            {
                Log.Error(ex, "Out of memory while fetching the log records");
                return BadRequest("Out of Memory: Der gewählte Zeitraum umfasst zu viele Daten-Einträge. Bitte wählen Sie einen kürzeren Zeitraum");
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return BadRequest(ex.Message);
            }
        }

        private HttpResponseMessage CreateExcelFile(List<PrimaerdatenReportRecord> response)
        {
            var retVal = new HttpResponseMessage(HttpStatusCode.OK);
            var file = $"Viaduc-Primaerdaten-{DateTime.Now:s}.xlsx";
            var contentType = MimeMapping.GetMimeMapping(Path.GetExtension(file));
            var format = new DateTimeFormat("dd.MM.yyyy HH:mm:ss");

            using (var stream = exportHelper.ExportToExcel(response, new ExcelColumnInfos
            {
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.OrderId), MakeAutoWidth = true},
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.VeId), MakeAutoWidth = true},
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.Size), MakeAutoWidth = true},
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.FileCount), MakeAutoWidth = true},
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.Source), MakeAutoWidth = true},
                new ExcelColumnInfo
                    {ColumnName = nameof(PrimaerdatenReportRecord.NeuEingegangen), FormatSpecification = format.FormatString, MakeAutoWidth = true},
                new ExcelColumnInfo
                    {ColumnName = nameof(PrimaerdatenReportRecord.FreigabePrüfen), FormatSpecification = format.FormatString, MakeAutoWidth = true},
                new ExcelColumnInfo
                    {ColumnName = nameof(PrimaerdatenReportRecord.FürDigitalisierungBereit), FormatSpecification = format.FormatString, MakeAutoWidth = true},
                new ExcelColumnInfo
                    {ColumnName = nameof(PrimaerdatenReportRecord.FürAushebungBereit), FormatSpecification = format.FormatString, MakeAutoWidth = true},
                new ExcelColumnInfo
                    {ColumnName = nameof(PrimaerdatenReportRecord.Ausgeliehen), FormatSpecification = format.FormatString, MakeAutoWidth = true},
                new ExcelColumnInfo
                    {ColumnName = nameof(PrimaerdatenReportRecord.ZumReponierenBereit), FormatSpecification = format.FormatString, MakeAutoWidth = true},
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.PrimaryDataLinkCreationDate), FormatSpecification = format.FormatString,
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.StartFirstSynchronizationAttempt), FormatSpecification = format.FormatString,
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.StartLastSynchronizationAttempt), FormatSpecification = format.FormatString,
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.CompletionLastSynchronizationAttempt), FormatSpecification = format.FormatString,
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.ClickButtonPrepareDigitalCopy), FormatSpecification = format.FormatString,
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.EstimatedPreparationTimeVeAccordingDetailPage), MakeAutoWidth = true},
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.StartFirstPreparationAttempt), FormatSpecification = format.FormatString,
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.StartLastPreparationAttempt), FormatSpecification = format.FormatString, MakeAutoWidth = true
                },
                new ExcelColumnInfo
                {
                    ColumnName = nameof(PrimaerdatenReportRecord.CompletionLastPreparationAttempt), FormatSpecification = format.FormatString,
                    MakeAutoWidth = true
                },
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.CountPreparationAttempts), MakeAutoWidth = true},
                new ExcelColumnInfo
                    {ColumnName = nameof(PrimaerdatenReportRecord.StorageUseCopyCache), FormatSpecification = format.FormatString, MakeAutoWidth = true},
                new ExcelColumnInfo {ColumnName = nameof(PrimaerdatenReportRecord.ShippingMailReadForDownload), MakeAutoWidth = true}
            }))
                retVal.Content = new ByteArrayContent(stream.ToArray());
            retVal.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            retVal.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = file
            };
            return retVal;
        }

        private static void SetMetaData(PrimaerdatenAufbereitungItem item, PrimaerdatenReportRecord record)
        {
            var list = JsonConvert.DeserializeObject<List<ElasticArchiveRecordPackage>>(item.PackageMetadata);
            var metaData = list.FirstOrDefault();
            if (metaData != null)
            {
                record.Size = metaData.SizeInBytes;
                record.FileCount = metaData.FileCount;
                record.EstimatedPreparationTimeVeAccordingDetailPage =
                    metaData.FulltextExtractionDuration + metaData.RepositoryExtractionDuration;
            }
        }

        private static void SetPrimaerdaten(PrimaerdatenReportRecord record, PrimaerdatenAufbereitungItem item)
        {
            // Primaerdaten
            record.OrderId = item.OrderItemId;
            record.VeId = item.VeId;
            record.Source = item.Quelle;
            record.NeuEingegangen = item.NeuEingegangen;
            record.FreigabePrüfen = item.FreigabePruefen;
            record.FürDigitalisierungBereit = item.FuerDigitalisierungBereit;
            record.FürAushebungBereit = item.FuerAushebungBereit;
            record.Ausgeliehen = item.Ausgeliegen;
            record.ZumReponierenBereit = item.ZumReponierenBereit;
            record.ClickButtonPrepareDigitalCopy = item.Registriert;
            record.StartFirstPreparationAttempt = item.ErsterAufbereitungsversuch;
            record.StartLastPreparationAttempt = item.LetzterAufbereitungsversuch;
            record.CompletionLastPreparationAttempt = item.Abgeschlossen;
            record.CountPreparationAttempts = item.AnzahlVersucheDownload;
            record.StorageUseCopyCache = item.ImCacheAbgelegt;
        }

        private List<PrimaerdatenReportRecord> ConvertToPrimaerdatenRecord(List<PrimaerdatenAufbereitungItem> rawResult, List<SyncInfoForReport> syncInfoForReport)
        {
            List<PrimaerdatenReportRecord> result = new List<PrimaerdatenReportRecord>();
            
            foreach (var item in rawResult)
            {
                var record = new PrimaerdatenReportRecord();
                if (syncInfoForReport != null && item.MutationsId.HasValue)
                {
                   var infoForReport = syncInfoForReport.FirstOrDefault(mut => mut.MutationId == item.MutationsId.Value);
                   if (infoForReport != null)
                   {
                       record.PrimaryDataLinkCreationDate = infoForReport.ErstellungsdatumPrimaerdatenVerbindung;
                       record.StartFirstSynchronizationAttempt = infoForReport.StartErsterSynchronisierungsversuch;
                       record.CompletionLastSynchronizationAttempt = infoForReport.AbschlussSynchronisierung;
                       record.StartLastSynchronizationAttempt = infoForReport.StartLetzterSynchronisierungsversuch;
                       record.CountSynchronizationAttempts = infoForReport.AnzahlNotwendigerSynchronisierungsversuche;
                   }
                }

                SetMetaData(item, record);
                SetPrimaerdaten(record, item);

                result.Add(record);
            }

            return result;
        }
    }
}
