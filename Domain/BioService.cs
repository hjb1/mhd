using System;
using Azure.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using mhd.Bio;

namespace mhd.Bio;

public class BioService: IBioService
{
    private readonly IDbContextFactory<DatabaseContext> factory;

    public BioService(IDbContextFactory<DatabaseContext> factory) =>
        this.factory = factory;

    public async Task<Bio> LoadDocumentAsync(string PerIdentification)
        {
            using var context = factory.CreateDbContext();
            return await context.Bio
                    .WithPartitionKey(PerIdentification)
                    .SingleOrDefaultAsync(d => d.PerIdentification == PerIdentification);
        }

    public async Task<List<BioSummary>> QueryDocumentAsync(
    
    )
    {
        using var context = factory.CreateDbContext();

        var result = new HashSet<BioSummary>();

        var documents = await context.Bio.ToListAsync();

        return documents.Select(d => new BioSummary(d)).OrderBy(ds => ds.PerIdentification).ToList();
    }

}