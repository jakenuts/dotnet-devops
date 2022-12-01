using System.Reflection;

namespace devops.Internal;

public static class Constants
{
    public static readonly int UnauthorizedExitCode = -1000;

    public const string SettingsAppName = "AE71EE95-49BD-40A9-81CD-B1DFD873EEA8";
    public const string SettingsFileName = "dotnet-devops.secrets.json";

    public static readonly string UserProfileDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static readonly string SettingsDirectory = Path.Combine(UserProfileDirectory, ".dotnet-devops");
    
    //Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    public static readonly string SettingsPath = Path.Combine(SettingsDirectory, SettingsFileName);

}