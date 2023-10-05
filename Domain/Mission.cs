using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace mhd.Domain;
[PrimaryKey(nameof(misMissionNo))]
public class Mission
{
    public string id { get; set; } 
    public string acAircraftNo {get; set; }
    public string misMissionNo { get; set; }
    public string acmPlaneRemarks { get; set; }
    public string acmAircraftPosition { get; set; }
    public string acmSquadron { get; set; }
    public string acmSquadronPos { get; set; }
    public string acmGroupPosition { get; set; }
    public string acmWingPos { get; set; }
    public string acmFormationSize { get; set; }
    public string acmAircraftStatus { get; set; }
    [ForeignKey(nameof(misMissionNo))]
    public List<MissionCrew> MissionCrew {get; set;} = new List<MissionCrew>();
}