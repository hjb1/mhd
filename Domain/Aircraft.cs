using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace mhd.Domain;
[PrimaryKey("acAircraftNo")]
public class Aircraft
{
    public string id { get; set; } 
    public string acAircraftNo { get; set; } 
    public string acAircraftLetter { get; set; }
    public string acAircraftName { get; set; }
    public string acACB { get; set; }
    public string acACI { get; set; }
    public string acACAO { get; set; }
    public string acACFD { get; set; }
    public string acOrigGroupAssignment { get; set; }
    public string acSquadron { get; set; }
    public string acFinalAircraftDisposition { get; set; }
    public string acBG { get; set; }
    [ForeignKey(nameof(acAircraftNo))]
    public List<Mission> Mission { get; set; }
}