using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using mhd;
using mhd.Domain;

namespace mhd.DataAccess;

public sealed class DatabaseContext : DbContext
{
    public const string PartitionKey = nameof(PartitionKey);

    public DatabaseContext(DbContextOptions<DatabaseContext> options)
        : base(options) 
        {}
    public DbSet<Bio> Bio { get; set; }
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
        BioModel.HasPartitionKey(d => d.perIdentification);

        var PersonnelModel = modelBuilder.Entity<Personnel>();

        PersonnelModel.ToContainer(nameof(Personnel)).HasNoDiscriminator();
        PersonnelModel.HasPartitionKey(d => d.perIdentification)
            .HasOne(d => d.Bio)
            .WithOne(b => b.Personnel)
            .HasForeignKey<Bio>(b => b.perIdentification)
            .HasPrincipalKey<Personnel>(d => d.perIdentification);
        
        var AircraftModel = modelBuilder.Entity<Aircraft>();

        AircraftModel.ToContainer(nameof(Aircraft)).HasNoDiscriminator();
        AircraftModel.HasPartitionKey(d => d.acAircraftNo);

        var MissionModel = modelBuilder.Entity<Mission>();

        MissionModel.ToContainer(nameof(Mission)).HasNoDiscriminator();
        MissionModel.HasPartitionKey(d => d.misMissionNo);

        var MissionCrewModel = modelBuilder.Entity<MissionCrew>();

        MissionCrewModel.ToContainer(nameof(MissionCrew)).HasNoDiscriminator();
        MissionCrewModel.HasPartitionKey(d => d.misMissionNo);


        base.OnModelCreating(modelBuilder);
    }
    public DbSet<Personnel> Personnel { get; set; }
    public DbSet<Aircraft> Aircraft {get; set; }
    public DbSet<Mission> Mission {get; set; }
    public DbSet<MissionCrew> MissionCrew {get; set; }
    
}