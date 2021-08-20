using System;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using Serilog;

namespace CMI.Engine.Asset
{
    public interface IAssetPreparationEngine
    {
        Task<PreprocessingResult> DetectAndFlagLargeDimensions(RepositoryPackage package, int primaerdatenAuftragId);
        Task<PreprocessingResult> OptimizePdfIfRequired(RepositoryPackage package, int primaerdatenAuftragId);
    }

    public class AssetPreparationEngine : IAssetPreparationEngine
    {
        public Task<PreprocessingResult> DetectAndFlagLargeDimensions(RepositoryPackage package, int primaerdatenAuftragId)
        {
            try
            {
                Log.Information("Starting to detect large dimensions in documents for primaerdatenAuftrag with id {PrimaerdatenAuftragId}", primaerdatenAuftragId);

                // Hier nur ein Beispiel wie ich gerne festhalten möchte, dass beim Sync die Files übersprungen werden sollen.
                var file = package.Files.FirstOrDefault();
                if (file != null)
                {
                    //file.SkipOCR = true;
                }

                return Task.FromResult(new PreprocessingResult(){Success = true});
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while detecting large dimensions in documents for primaerdatenAuftrag with id {PrimaerdatenAuftragId}", primaerdatenAuftragId);
                return Task.FromResult(new PreprocessingResult() { Success = false, ErrorMessage = "Unexpected error while detecting large dimensions in documents."});
            }
        }

        public Task<PreprocessingResult> OptimizePdfIfRequired(RepositoryPackage package, int primaerdatenAuftragId)
        {
            try
            {
                Log.Information("Starting to detect and optimize PDF for primaerdatenAuftrag with id {PrimaerdatenAuftragId}", primaerdatenAuftragId);
                return Task.FromResult(new PreprocessingResult() { Success = true });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while detect and optimize PDF for primaerdatenAuftrag with id {PrimaerdatenAuftragId}", primaerdatenAuftragId);
                return Task.FromResult(new PreprocessingResult() { Success = true, ErrorMessage = "Unexpected error while detect and optimize PDF."});
            }
        }
    }
}
