namespace Subchron.API.Models.Settings;

public class CloudinarySettings
{
    public string CloudName { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";
    public string? Folder { get; set; } = "Home/Subchron";
}