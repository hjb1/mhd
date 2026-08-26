namespace mhd.Domain;

public sealed class PersonPicture
{
    public string Kind { get; set; } = string.Empty;
    public string Stem { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string BmpUrl { get; set; } = string.Empty;
    public string EnhancedUrl { get; set; } = string.Empty;

    public string Label => Kind switch
    {
        "before" => "Before",
        "after" => "After",
        "crew" => "Crew",
        "photo" => "Photo",
        _ => Kind
    };
}