public class ApplicationSettings
{
    public const string SettingsSectionName = "Settings";
    public string? ServiceUrl { get; set; }
    public string? RabbitServer { get; set; }
    public string? RabbitUser { get; set; } 
    public string? RabbitPassword { get; set; } 
}