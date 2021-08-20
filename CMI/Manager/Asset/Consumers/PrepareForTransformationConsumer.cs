using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Common.Gebrauchskopie;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Engine.Asset;
using CMI.Manager.Asset.ParameterSettings;
using CMI.Manager.Asset.Properties;
using MassTransit;
using Serilog;
using Serilog.Context;
using Serilog.Core.Enrichers;

namespace CMI.Manager.Asset.Consumers
{
    public class PrepareForTransformationConsumer : IConsumer<PrepareForTransformationMessage>
    {
        private readonly IAssetManager assetManager;
        private readonly IScanProcessor scanProcessor;
        private readonly ITransformEngine transformEngine;
        private readonly IParameterHelper parameterHelper;
        private readonly IAssetPreparationEngine assetPreparationEngine;
        private List<Func<PrepareForTransformationMessage, Task<PreprocessingResult>>> preparationSteps;

        public PrepareForTransformationConsumer(IAssetManager assetManager, IScanProcessor scanProcessor, ITransformEngine transformEngine, IParameterHelper parameterHelper, IAssetPreparationEngine assetPreparationEngine)
        {
            this.assetManager = assetManager;
            this.scanProcessor = scanProcessor;
            this.transformEngine = transformEngine;
            this.parameterHelper = parameterHelper;
            this.assetPreparationEngine = assetPreparationEngine;
            preparationSteps = new List<Func<PrepareForTransformationMessage, Task<PreprocessingResult>>>();
        }

        public async Task Consume(ConsumeContext<PrepareForTransformationMessage> context)
        {
            var conversationEnricher = new PropertyEnricher(nameof(context.ConversationId), context.ConversationId);
            var archiveRecordIdEnricher = new PropertyEnricher(nameof(context.Message.RepositoryPackage.ArchiveRecordId), context.Message.RepositoryPackage.ArchiveRecordId);
            var packageIdEnricher = new PropertyEnricher(nameof(context.Message.RepositoryPackage.PackageId), context.Message.RepositoryPackage.PackageId);

            using (LogContext.Push(conversationEnricher, archiveRecordIdEnricher, packageIdEnricher))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(PrepareForTransformationMessage), context.ConversationId);

                // 1. Step: Extract Zip file(s)
                preparationSteps.Add(ExtractRepositoryPackage);
                // 2. Step: ConvertAreldaXml
                preparationSteps.Add(ConvertAreldaMetadataXml);
                // 3. Step: ConvertJp2ToPdf
                preparationSteps.Add(ConvertSingleJp2ToPdf);
                // 4. Step: Detect and mark files with large dimensions
                preparationSteps.Add(DetectAndFlagLargeDimensions);
                // 5. Step: Detect and optimize pdf files that need optimization
                preparationSteps.Add(DetectAndOptimizePdf);


                foreach (var step in preparationSteps)
                {
                    var result = await step(context.Message);

                    // In case any step was not successful, fail the sync
                    if (!result.Success)
                    {
                        await PublishAssetReadyFailed(context, result.ErrorMessage);
                        return;
                    }
                }

                // Forward the prepared data to the next processing point
                var endpoint = await context.GetSendEndpoint(new Uri(context.SourceAddress, BusConstants.AssetManagerTransformAssetMessageQueue));

                await endpoint.Send(new TransformAsset
                {
                    AssetType = context.Message.AssetType,
                    OrderItemId = context.Message.OrderItemId,
                    CallerId = context.Message.CallerId,
                    RetentionCategory = context.Message.RetentionCategory,
                    Recipient = context.Message.Recipient,
                    Language = context.Message.Language,
                    ProtectWithPassword = context.Message.ProtectWithPassword,
                    PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId,
                    RepositoryPackage= context.Message.RepositoryPackage
                });
            }
        }

        /// <summary>
        /// In case we have a Benutzungskopie, we need to transform the metadata.xml
        /// </summary>
        /// <param name="message"></param>
        private Task<PreprocessingResult> ConvertAreldaMetadataXml(PrepareForTransformationMessage message)
        {
            try
            {
                var tempFolder = GetTempFolder(message);

                if (message.AssetType == AssetType.Benutzungskopie)
                {
                    transformEngine.ConvertAreldaMetadataXml(tempFolder);
                }

                return Task.FromResult(new PreprocessingResult {Success = true});
            }
            catch (Exception ex)
            {
                var msg = "Unexpected error while converting to AreldaMetadata xml.";
                Log.Error(ex, msg);
                return Task.FromResult(new PreprocessingResult {Success = false, ErrorMessage = msg});
            }
        }

        private Task<PreprocessingResult> DetectAndFlagLargeDimensions(PrepareForTransformationMessage message)
        {
            try
            {
                // do the detection
                assetPreparationEngine.DetectAndFlagLargeDimensions(message.RepositoryPackage, message.PrimaerdatenAuftragId);

                return Task.FromResult(new PreprocessingResult { Success = true });
            }
            catch (Exception ex)
            {
                var msg = "Unexpected error while detecting large dimensions.";
                Log.Error(ex, msg);
                return Task.FromResult(new PreprocessingResult { Success = false, ErrorMessage = msg });
            }
        }

        private Task<PreprocessingResult> DetectAndOptimizePdf(PrepareForTransformationMessage message)
        {
            try
            {
                // Now do the optimization
                assetPreparationEngine.OptimizePdfIfRequired(message.RepositoryPackage, message.PrimaerdatenAuftragId);

                return Task.FromResult(new PreprocessingResult { Success = true });
            }
            catch (Exception ex)
            {
                var msg = "Unexpected error while detecting and optimizing pdf.";
                Log.Error(ex, msg);
                return Task.FromResult(new PreprocessingResult { Success = false, ErrorMessage = msg });
            }
        }

        private Task<PreprocessingResult> ConvertSingleJp2ToPdf(PrepareForTransformationMessage message)
        {
            try
            {
                var tempFolder = GetTempFolder(message);

                var metadataFile = Path.Combine(tempFolder, "header", "metadata.xml");
                var paket = (PaketDIP)Paket.LoadFromFile(metadataFile);

                // Create pdf documents from scanned jpeg 2000 scans.
                scanProcessor.ConvertSingleJpeg2000ScansToPdfDocuments(paket, tempFolder,
                    parameterHelper.GetSetting<ScansZusammenfassenSettings>());

                return Task.FromResult(new PreprocessingResult { Success = true });
            }
            catch (Exception ex)
            {
                var msg = "Unexpected error while converting single jpeg 2000 to pdf.";
                Log.Error(ex, msg);
                return Task.FromResult(new PreprocessingResult { Success = false, ErrorMessage = msg });
            }
        }

        private static string GetTempFolder(PrepareForTransformationMessage message)
        {
            var packageFileName = Path.Combine(Settings.Default.PickupPath, message.RepositoryPackage.PackageFileName);
            var fi = new FileInfo(packageFileName);
            var tempFolder = Path.Combine(fi.DirectoryName ?? throw new InvalidOperationException(), fi.Name.Remove(fi.Name.Length - fi.Extension.Length));
            return tempFolder;
        }

        private async Task PublishAssetReadyFailed(ConsumeContext<PrepareForTransformationMessage> context, string errorMessage)
        {
            try
            {
                var message = context.Message;
                // If we do have an error, we stop the sync process here.
                Log.Error(
                    "Failed to preprocess the package for transformation for archiveRecord {archiveRecordId} with conversationId {ConversationId}",
                    message.RepositoryPackage.ArchiveRecordId, context.ConversationId);

                var assetReady = new AssetReady
                {
                    ArchiveRecordId = message.RepositoryPackage.ArchiveRecordId,
                    OrderItemId = message.OrderItemId,
                    CallerId = message.CallerId,
                    AssetType = message.AssetType,
                    Recipient = message.Recipient,
                    RetentionCategory = message.RetentionCategory,
                    PrimaerdatenAuftragId = message.PrimaerdatenAuftragId,
                    // We have failed in the conversion inform the world about the failed package
                    Valid = false,
                    ErrorMessage = errorMessage
                };

                await assetManager.UpdatePrimaerdatenAuftragStatus(new UpdatePrimaerdatenAuftragStatus
                {
                    PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId,
                    Service = AufbereitungsServices.AssetService,
                    Status = AufbereitungsStatusEnum.PreprocessingAbgeschlossen,
                    ErrorText = errorMessage
                });

                // inform the world about the failed package
                await context.Publish<IAssetReady>(assetReady);

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while publish a failed AssetReady event.");
            }
            finally
            {
                // Delete the temp files
                var tempFolder = GetTempFolder(context.Message);
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
        }

        private async Task<PreprocessingResult> ExtractRepositoryPackage(PrepareForTransformationMessage message)
        {
            var packageFileName = message.RepositoryPackage.PackageFileName;
            var primaerdatenAuftragId = message.PrimaerdatenAuftragId;

            var success = await assetManager.ExtractZipFile(new ExtractZipArgument
            {
                PackageFileName = packageFileName,
                PrimaerdatenAuftragId = primaerdatenAuftragId
            });

            return success ? new PreprocessingResult {Success = true} : 
                             new PreprocessingResult {Success = false, ErrorMessage = $"Failed to unzip package {packageFileName}"};
        }
    }
}