using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace mhd.Domain
{
    public interface IMHDService
    {
        Task<List<BioSummary>> QueryDocumentAsync();
        Task<Bio> LoadBioAsync(string PerIdentification);
        Task<List<PersonnelSummary>> QueryPersonnelAsync();
        Task FillPersonnelAsync(List<PersonnelSummary> destination, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
        Task<List<Aircraft>> QueryAircraftAsync();
        Task FillAircraftAsync(List<Aircraft> destination, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
        Task<Aircraft> LoadAircraftMissionCrewSummaryAsync(string aircraftNo);
        void InvalidateListCache();
    }
}
