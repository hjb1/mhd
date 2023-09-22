using System;
using System.Collections.Generic;
using mhd;

namespace mhd.Domain;
public class Personnel
{
    public string id { get; set; }
    public string DeceasedDate { get; set; }
    public string ObituaryComments { get; set; }
    public string perASN { get; set; }
    public string perAwards { get; set; }
    public string perBurialCountry { get; set; }
    public string perCenetery { get; set; }
    public string perFirstName { get; set; }
    public bool perFlight { get; set; }
    public string perGroup { get; set; }
    public string perIdentification { get; set; }
    public string perKIA { get; set; }
    public string perLastName { get; set; }
    public string perMIA { get; set; }
    public string perMiddleName { get; set; }
    public string perPlot { get; set; }
    public string perPOW { get; set; }
    public string perSquadron { get; set; }
    public string perWWIIAddress { get; set; }
}

public interface IPersonnelSummary
{
    List<PersonnelSummary> Personnel {get;}
   
}
public interface IPersonnelService
{
     Task<List<PersonnelSummary>> QueryDocumentAsync(
    
    );
    Task<Personnel> LoadDocumentAsync(string PerIdentification);
}