namespace Rating.Cli;

public class ApplicationSettings
{
    public const string SettingsSectionName = "Settings";
    public string? ServiceUrl { get; set; }

    public TimeSpan? MinInterval { get; set; }
    public TimeSpan? MaxInterval { get; set; }
}