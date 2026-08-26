namespace mhd.Domain;

public class MissionTarget
{
    public string id { get; set; } = string.Empty;
    public string acBG { get; set; } = string.Empty;
    public string misMissionNo { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Target { get; set; }
    public string? MissionDate { get; set; }
    public string? Narrative { get; set; }
}
