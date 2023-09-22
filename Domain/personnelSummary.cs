namespace mhd.Domain
{
    public class PersonnelSummary
    {
        public PersonnelSummary()
        {

        }
        public PersonnelSummary(Personnel Personnel)
        {
            Id = Personnel.id;
            PerIdentification = Personnel.perIdentification;
            LastName = Personnel.perLastName;
            FirstName = Personnel.perFirstName;
        }
        public string Id { get; set; } = string.Empty;
        public string PerIdentification { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
    }
}