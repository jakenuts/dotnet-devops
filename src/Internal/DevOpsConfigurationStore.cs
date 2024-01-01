using System.Text.Json;

namespace devops.Internal;

public class DevOpsConfigurationStore(DevOpsConfigurationProtector protector)
{
    public void SaveConfiguration(DevOpsConfiguration config, string path)
    {
        var saveConfig = new DevOpsConfiguration
        {
            CollectionPAT = config.CollectionPAT,
            CollectionUri = config.CollectionUri
        };

        protector.Encrypt(saveConfig);

        var configAsJson = JsonSerializer.Serialize(config);
        var configFile = "{\"DevOps\":" + configAsJson + "\n}";

        if (!Directory.Exists(Constants.SettingsDirectory))
        {
            Directory.CreateDirectory(Constants.SettingsDirectory);
        }

        File.WriteAllText(path, configFile);
    }
}