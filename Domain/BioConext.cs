using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using mhd;
using mhd.Bio;

namespace mhd.Bio;

public sealed class DatabaseContext : DbContext
{
    public const string PartitionKey = nameof(PartitionKey);

    public DatabaseContext(DbContextOptions<DatabaseContext> options)
        : base(options) 
        {}
    public DbSet<Bio> Bio { get; set; } = null;
    public static string ComputePartitionKey<T>()
            where T : class, IBioSummary => typeof(T).FullName;
    
    public void SetPartitionKey<T>(T entity)
            where T : class, IBioSummary =>
                Entry(entity).Property(PartitionKey).CurrentValue =
                    ComputePartitionKey<T>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var BioModel = modelBuilder.Entity<Bio>();

        BioModel.ToContainer(nameof(Bio)).HasNoDiscriminator();
        BioModel.HasPartitionKey(d => d.PerIdentification);

        base.OnModelCreating(modelBuilder);
    }
}