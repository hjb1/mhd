using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Radzen;

namespace mhd.Domain
{
public interface IMHDService
{
     Task<List<BioSummary>> QueryDocumentAsync(
    
    );
    Task<Bio> LoadBioAsync(string PerIdentification);

    Task<List<PersonnelSummary>> QueryPersonnelAsync();
    Task<List<Aircraft>> QueryAircraftAsync();
    
}

}