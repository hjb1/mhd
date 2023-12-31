namespace mhd.Domain
{
    public class PersonnelSummary
    {
        public PersonnelSummary()
        {

        }
        public PersonnelSummary(Personnel Personnel, bool bio)
        {
            Id = Personnel.id;
            PerIdentification = Personnel.perIdentification;
            LastName = Personnel.perLastName;
            FirstName = Personnel.perFirstName;
            PerGroup = Personnel.perGroup;
            PerSquadron = Personnel.perSquadron;
            HasBio = bio;
            DeceasedDate = Personnel.DeceasedDate;
            ObituaryComments = Personnel.ObituaryComments;
        }
        public string Id { get; set; } = string.Empty;
        public string PerIdentification { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string PerGroup { get; set; } = string.Empty;
        public string PerSquadron { get; set; } = string.Empty;
        public string DeceasedDate { get; set; } = string.Empty;
        public string ObituaryComments { get; set; } = string.Empty;
        public bool HasBio {get; set; } = false; 
    }
}