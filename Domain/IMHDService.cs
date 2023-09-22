using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mhd.Domain
{
public interface IMHDService
{
     Task<List<BioSummary>> QueryDocumentAsync(
    
    );
    Task<Bio> LoadDocumentAsync(string PerIdentification);

    Task<List<PersonnelSummary>> QueryPersonnelAsync(
        
    );
    
}

}