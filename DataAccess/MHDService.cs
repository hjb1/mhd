using System;
using Azure.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using mhd.Domain;
using Radzen;
using System.ComponentModel.DataAnnotations;

namespace mhd.DataAccess;

public class MHDService : IMHDService
{
    private readonly IDbContextFactory<DatabaseContext> factory;

    public MHDService(IDbContextFactory<DatabaseContext> factory) =>
        this.factory = factory;

    public async Task<Bio> LoadBioAsync(string perIdentification)
        {
            using var context = factory.CreateDbContext();
            Bio? bio = await context.Bio
                    .WithPartitionKey(perIdentification)
                    .SingleOrDefaultAsync(d => d.perIdentification == perIdentification);
            
            
            return bio;
        }

    public async Task<List<BioSummary>> QueryDocumentAsync(
    
    )
    {
        using var context = factory.CreateDbContext();

        var result = new HashSet<BioSummary>();

        var documents = await context.Bio.ToListAsync();

        return documents.Select(d => new BioSummary(d)).OrderBy(ds => ds.PerIdentification).ToList();
    }
    public async Task<List<PersonnelSummary>> QueryPersonnelAsync()
    {
        using var context = factory.CreateDbContext();

        var result = new HashSet<PersonnelSummary>();
        bool HasBio = false;

        var nonNullBios = await context.Bio
            .Where(b => b.perIdentification != null)
            .Select(b => b.perIdentification)
            .Distinct()
            .ToListAsync();

        var personnel = await context.Personnel.ToListAsync();
        

        return personnel.Select(d => 
            {
                bool hasBio = nonNullBios.Any(bioID => bioID == d.perIdentification);
                return new PersonnelSummary(d, hasBio);
            })
            .OrderBy(ds => ds.LastName)
            .ToList();
    }
    public async Task<List<Aircraft>> QueryAircraftAsync()
    {
        using var context = factory.CreateDbContext();

        List<Aircraft> aircraftList = await context.Aircraft.ToListAsync();

        foreach (Aircraft aircraft in aircraftList)
        {
            if (aircraft.acFinalAircraftDisposition =="Aircraft Final Disposition")
            {
                aircraft.acFinalAircraftDisposition = "";
            }
        }

        return aircraftList.OrderBy(d => d.acAircraftNo).ToList();
    }
 
}