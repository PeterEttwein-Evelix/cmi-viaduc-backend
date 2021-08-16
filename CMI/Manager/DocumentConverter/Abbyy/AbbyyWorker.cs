using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CMI.Manager.DocumentConverter.Extraction;
using CMI.Manager.DocumentConverter.Extraction.Interfaces;
using CMI.Manager.DocumentConverter.Render;
using FREngine;
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

        public AbbyyWorker(IEnginesPool enginesPool)
        {
            this.enginesPool = enginesPool;
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
            }
            finally
            {
                enginesPool.ReleaseEngine(engine, isRecycleRequired);
                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }
            }

            return retVal;
        }

        public TransformResult TransformDocument(string profile, FileInfo sourceFile, FileInfo targetFile)
        {
            var retVal = new TransformResult();

            var engine = enginesPool.GetEngine();
            bool isRecycleRequired = false;

            try
            {
                IFRDocument fineReaderDocument = null;
                try
                {
                    fineReaderDocument = LoadFineReaderDocument(sourceFile.FullName, profile, engine);
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
            }
            finally
            {
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
    }
}
