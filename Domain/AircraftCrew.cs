using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace mhd.Domain;
public class AircraftCrew
{
    public string id { get; set; } 
    [ForeignKey("Aircraft")]
    public Aircraft acAircraftNo { get; set; } 
    [ForeignKey("Personnel")]
    public Personnel perIdentification { get; set; }
    public string accRank { get; set; }
    public string accDuties { get; set; }
    public string accComments { get; set; }
}