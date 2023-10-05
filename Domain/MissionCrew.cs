using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Policy;
using Microsoft.EntityFrameworkCore;

namespace mhd.Domain;
[PrimaryKey("misMissionNo")]
public class MissionCrew
{
    public string? id { get; set; }
    public string acAircraftNo {get; set; }
    public string misMissionNo { get; set; }
    [ForeignKey("perIdentification")]
    public Personnel? Personnel { get; set; } = new Personnel();
    public string? perIdentification {get; set; }
    public string? Status { get; set; }
    public string? Rank { get; set; }
    public string? Duties { get; set; }
    public string? SerialNumber { get; set; }
    public string? NameRemarks { get; set; }
    public string? PlaneRemarks { get; set; }
    public string? ASN { get; set; }
    public string? Address { get; set; }
    public string? Parachuted { get; set; }
    public string? Recording { get; set; }
    public string? Video { get; set; }
    public string? misFlak { get; set; }
    public string? misFighterSupport { get; set; }
    public string? misEnemyFighters { get; set; }
    public string? misAircraftDamage { get; set; }
    public string? misMissionLength { get; set; }
}