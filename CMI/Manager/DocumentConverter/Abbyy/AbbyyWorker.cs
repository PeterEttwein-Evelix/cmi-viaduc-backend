﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using CMI.Contract.DocumentConverter;
using CMI.Manager.DocumentConverter.Extraction;
using CMI.Manager.DocumentConverter.Extraction.Interfaces;
using CMI.Manager.DocumentConverter.Render;
using FREngine;
using MassTransit;
using Serilog;

namespace CMI.Manager.DocumentConverter.Abbyy
{
    public interface IAbbyyWorker
    {
        ExtractionResult ExtractTextFromDocument(string inputFile, ITextExtractorSettings settings);
        TransformResult TransformDocument(string profile, FileInfo sourceFile, FileInfo targetFile);
    }

    public class AbbyyWorker : IAbbyyWorker
    {
        private readonly IEnginesPool enginesPool;
        private readonly IBus bus;
        private string sourceFile;
        private int lastPercentageNumber = -1;
        private long lastPercentageLogTime;

        public static List<string> profiles = new List<string>
        {
            "TextExtraction_Accuracy",
            "TextExtraction_Speed",
            "DocumentArchiving_Accuracy",
            "DocumentArchiving_Speed",
            "BookArchiving_Accuracy",
            "BookArchiving_Speed",
            "DocumentConversion_Accuracy",
            "DocumentConversion_Speed"
        };

        public AbbyyWorker(IEnginesPool enginesPool, IBus bus)
        {
            this.enginesPool = enginesPool;
            this.bus = bus;
        }

        public ExtractionResult ExtractTextFromDocument(string inputFile, ITextExtractorSettings settings)
        {
            var retVal = new ExtractionResult(settings.MaxExtractionSize);
            var outputFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");

            var engine = enginesPool.GetEngine();
            bool isRecycleRequired = false;

            try
            {
                IFRDocument fineReaderDocument = null;
                try
                {
                    fineReaderDocument = LoadFineReaderDocument(inputFile, settings.TextExtractionProfile, engine);
                    SubscribeExtractionEvents((FRDocument) fineReaderDocument);
                    fineReaderDocument.Process();

                    // Leerseiten überspringen bei einseitigen Dokumenten. 
                    if (fineReaderDocument.Pages.Count == 1)
                    {
                        // IsEmpty nutzt die Einstellungen die über das Profil festgelegt wurden
                        if (fineReaderDocument.Pages[0].IsEmpty())
                        {
                            Log.Information("The page {inputFile} was detected as empty.", inputFile);
                            return retVal;
                        }
                    }

                    fineReaderDocument.Export(outputFile, FileExportFormatEnum.FEF_TextUnicodeDefaults, null);

                    // Read the contents of the exported file
                    using (var sr = new StreamReader(outputFile))
                    {
                        while (sr.Peek() >= 0)
                        {
                            retVal.Append(sr.ReadLine());
                            if (retVal.LimitExceeded)
                            {
                                break;
                            }
                        }
                    }

                    // Alles OK
                    return retVal;
                }
                finally
                {
                    if (fineReaderDocument != null)
                    {
                        UnsubscribeExtractionEvents((FRDocument) fineReaderDocument);
                        fineReaderDocument.Close();
                        if (Marshal.IsComObject(fineReaderDocument))
                        {
                            Marshal.ReleaseComObject(fineReaderDocument);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Abbyy Textextraction failed: {ex.Message}");
                isRecycleRequired = enginesPool.ShouldRestartEngine(ex);
                retVal.HasError = true;
                retVal.ErrorMessage = ex.Message;

                // Push an error message to indicate that the item has failed
                bus.Publish<AbbyyProgressEvent>(new
                {
                    __TimeToLive = TimeSpan.FromSeconds(3),
                    File = sourceFile,
                    Process = ProcessType.TextExtraction,
                    EventType = AbbyyEventType.AbbyyOnProgressEvent,
                    HasFailed = true
                });
            }
            finally
            {
                // Push the final message to indicate that the item is finished
                bus.Publish<AbbyyProgressEvent>(new
                {
                    __TimeToLive = TimeSpan.FromSeconds(3),
                    File = sourceFile,
                    IsComplete = true,
                    Percentage = 100,
                    Process = ProcessType.TextExtraction,
                    EventType = AbbyyEventType.AbbyyOnProgressEvent
                });
                
                enginesPool.ReleaseEngine(engine, isRecycleRequired);
                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }
            }

            return retVal;
        }

        public TransformResult TransformDocument(string profile, FileInfo inputFile, FileInfo targetFile)
        {
            var retVal = new TransformResult();

            var engine = enginesPool.GetEngine();
            bool isRecycleRequired = false;

            try
            {
                IFRDocument fineReaderDocument = null;
                try
                {
                    fineReaderDocument = LoadFineReaderDocument(inputFile.FullName, profile, engine);
                    SubscribeTransformEvents((FRDocument) fineReaderDocument);
                    fineReaderDocument.Process();

                    fineReaderDocument.Export(targetFile.FullName, FileExportFormatEnum.FEF_PDF, null);

                    // Alles OK
                    retVal.TargetFile = targetFile;
                    retVal.TargetFile.Refresh();
                    return retVal;
                }
                finally
                {
                    if (fineReaderDocument != null)
                    {
                        UnsubscribeTransformEvents((FRDocument) fineReaderDocument);
                        fineReaderDocument.Close();
                        if (Marshal.IsComObject(fineReaderDocument))
                        {
                            Marshal.ReleaseComObject(fineReaderDocument);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Abbyy document conversion failed: {ex.Message}");
                isRecycleRequired = enginesPool.ShouldRestartEngine(ex);
                retVal.HasError = true;
                retVal.ErrorMessage = ex.Message;

                // Push an error message to indicate that the item has failed
                bus.Publish<AbbyyProgressEvent>(new
                {
                    __TimeToLive = TimeSpan.FromSeconds(3),
                    File = sourceFile,
                    Process = ProcessType.Rendering,
                    EventType = AbbyyEventType.AbbyyOnProgressEvent,
                    HasFailed = true
                });
            }
            finally
            {
                // Push the final message to indicate that the item is finished
                bus.Publish<AbbyyProgressEvent>(new
                {
                    __TimeToLive = TimeSpan.FromSeconds(3),
                    File = sourceFile,
                    IsComplete = true,
                    Percentage = 100,
                    Process = ProcessType.Rendering,
                    EventType = AbbyyEventType.AbbyyOnProgressEvent
                });
                enginesPool.ReleaseEngine(engine, isRecycleRequired);
            }

            return retVal;
        }

        private FRDocument LoadFineReaderDocument(string inputFile, string profile, IEngine engine)
        {
            var customProfile = Path.Combine(new FileInfo(this.GetType().Assembly.Location).DirectoryName, "AbbyyProfile.ini");
            CheckIfProfileIsValid(profile);
            CheckLicence(engine);
            engine.LoadPredefinedProfile(profile);
            if (File.Exists(customProfile))
            {
                engine.LoadProfile(customProfile);
            }
            else
            {
                Log.Warning("Custom profile for Abbyy not found. Make sure a file AbbyyProfile.ini is in the installation directory of the DocumentConverter service.");
            }

            var fineReaderDocument = engine.CreateFRDocumentFromImage(inputFile);
            sourceFile = inputFile;
            return fineReaderDocument;
        }

        private static void CheckIfProfileIsValid(string profile)
        {
            // Check if a valid profile was passed.
            if (!profiles.Exists(p => p == profile))
            {
                throw new ArgumentOutOfRangeException($"Ungültiges Profil <{profile}> für Textextraktion angegeben.");
            }

        }

        private static void CheckLicence(IEngine engine)
        {
            var remainingPages = engine.CurrentLicense.VolumeRemaining[LicenseCounterTypeEnum.LCT_Pages];
            if (remainingPages < 1)
            {
                throw new Exception("Anzahl Dokumente überschritten");
            }
        }

        #region Text Extraction Events
        private void SubscribeExtractionEvents(FRDocument fineReaderDocument)
        {
            fineReaderDocument.OnPageProcessed += TextExtractionOnPageProcessed;
            fineReaderDocument.OnProgress += TextExtractionOnProgress;
            fineReaderDocument.OnWarning += TextExtractionOnWarning;
        }

        private void UnsubscribeExtractionEvents(FRDocument fineReaderDocument)
        {
            fineReaderDocument.OnPageProcessed -= TextExtractionOnPageProcessed;
            fineReaderDocument.OnProgress -= TextExtractionOnProgress;
            fineReaderDocument.OnWarning -= TextExtractionOnWarning;
        }

        private void TextExtractionOnWarning(FRDocument sender, int pageIndex, string warning, ref bool cancel)
        {
            Log.Warning("Abbyy text extraction warning info for {sourceFile} - warning message is <{warning}> on page {pageIndex}", sourceFile, warning, pageIndex + 1);
        }

        private void TextExtractionOnProgress(FRDocument sender, int percentage, ref bool cancel)
        {
            // If there is no change, we do not want to "overload" the log
            // But log something at least every 30 seconds
            if (lastPercentageNumber == percentage && TimeSpan.FromTicks(DateTime.Now.Ticks - lastPercentageLogTime).Seconds < 30)
            {
                return;
            }

            Log.Information("Abbyy text extraction progress info for {sourceFile} - Percentage done is {percentage}", sourceFile, percentage);
            bus.Publish<AbbyyProgressEvent>(new 
            {
                __TimeToLive = TimeSpan.FromSeconds(3),
                File = sourceFile,
                Percentage = percentage,
                Process = ProcessType.TextExtraction,
                EventType = AbbyyEventType.AbbyyOnProgressEvent
            });
            lastPercentageNumber = percentage;
            lastPercentageLogTime = DateTime.Now.Ticks;
        }

        private void TextExtractionOnPageProcessed(FRDocument sender, int pageIndex, PageProcessingStageEnum processingStage)
        {
            Log.Information("Abbyy text extraction process info for {sourceFile} - stage is {processingStage} and current page index is {pageIndex}", sourceFile, processingStage, pageIndex + 1);
            bus.Publish<AbbyyProgressEvent>(new 
            {
                __TimeToLive = TimeSpan.FromSeconds(3),
                File = sourceFile,
                Page = pageIndex +1,
                TotalPages = sender.Pages.Count,
                Process = ProcessType.TextExtraction,
                EventType = AbbyyEventType.AbbyyOnPageEvent
            });
        }
        #endregion

        #region Transform events
        private void SubscribeTransformEvents(FRDocument fineReaderDocument)
        {
            fineReaderDocument.OnPageProcessed += TransformOnPageProcessed;
            fineReaderDocument.OnProgress += TransformOnProgress;
            fineReaderDocument.OnWarning += TransformOnWarning;
        }

        private void UnsubscribeTransformEvents(FRDocument fineReaderDocument)
        {
            fineReaderDocument.OnPageProcessed -= TransformOnPageProcessed;
            fineReaderDocument.OnProgress -= TransformOnProgress;
            fineReaderDocument.OnWarning -= TransformOnWarning;
        }

        private void TransformOnWarning(FRDocument sender, int pageIndex, string warning, ref bool cancel)
        {
            Log.Warning("Abbyy transform warning info for {sourceFile} - warning message is <{warning}> on page {pageIndex}", sourceFile, warning, pageIndex + 1);
        }

        private void TransformOnProgress(FRDocument sender, int percentage, ref bool cancel)
        {
            // If there is no change, we do not want to "overload" the log
            // But log something at least every 30 seconds
            if (lastPercentageNumber == percentage && TimeSpan.FromTicks(DateTime.Now.Ticks - lastPercentageLogTime).Seconds < 30)
            {
                return;
            }

            Log.Information("Abbyy transform progress info for {sourceFile} - Percentage done is {percentage}", sourceFile, percentage);
            bus.Publish<AbbyyProgressEvent>(new
            {
                __TimeToLive = TimeSpan.FromSeconds(3),
                File = sourceFile,
                Percentage = percentage,
                Process = ProcessType.Rendering,
                EventType = AbbyyEventType.AbbyyOnProgressEvent
            });
            lastPercentageNumber = percentage;
            lastPercentageLogTime = DateTime.Now.Ticks;
        }

        private void TransformOnPageProcessed(FRDocument sender, int pageIndex, PageProcessingStageEnum processingStage)
        {
            Log.Information("Abbyy transform process info for {sourceFile} - stage is {processingStage} and current page index is {pageIndex}", sourceFile, processingStage, pageIndex + 1);
            bus.Publish<AbbyyProgressEvent>(new
            {
                __TimeToLive = TimeSpan.FromSeconds(3),
                File = sourceFile,
                Page = pageIndex + 1,
                TotalPages = sender.Pages.Count,
                Process = ProcessType.Rendering,
                EventType = AbbyyEventType.AbbyyOnPageEvent
            });
        }
        #endregion

    }
}
