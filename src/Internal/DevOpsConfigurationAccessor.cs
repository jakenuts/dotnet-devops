using Microsoft.Extensions.Options;

namespace devops.Internal;

public class DevOpsConfigurationAccessor(IOptionsSnapshot<DevOpsConfiguration> settings)
{
    private static DevOpsConfiguration? _overrideSettings;

    public DevOpsConfiguration GetSettings() => _overrideSettings ?? settings.Value;

    public void UpdateSettings(DevOpsConfiguration updatedSettings)
    {
        _overrideSettings = updatedSettings;
    }
}