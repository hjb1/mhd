using System;
using Azure.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using mhd.Domain;

namespace mhd.DataAccess;

public class MHDService : IMHDService
{
    private readonly IDbContextFactory<DatabaseContext> factory;

    public MHDService(IDbContextFactory<DatabaseContext> factory) =>
        this.factory = factory;

    public async Task<Bio> LoadDocumentAsync(string perIdentification)
        {
            using var context = factory.CreateDbContext();
            return await context.Bio
                    .WithPartitionKey(perIdentification)
                    .SingleOrDefaultAsync(d => d.perIdentification == perIdentification);
        }

    public async Task<List<BioSummary>> QueryDocumentAsync(
    
    )
    {
        using var context = factory.CreateDbContext();

        var result = new HashSet<BioSummary>();

        var documents = await context.Bio.ToListAsync();

        return documents.Select(d => new BioSummary(d)).OrderBy(ds => ds.PerIdentification).ToList();
    }
    public async Task<List<PersonnelSummary>> QueryPersonnelAsync(

    )
    {
        using var context = factory.CreateDbContext();

        var result = new HashSet<PersonnelSummary>();

        var documents = await context.Personnel.ToListAsync();

        return documents.Select(d => new PersonnelSummary(d)).OrderBy(ds => ds.LastName).ToList();
    }

}