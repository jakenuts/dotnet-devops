using Microsoft.Extensions.Options;

namespace devops.Internal;

public class DevOpsConfigurationAccessor
{
    private readonly IOptionsSnapshot<DevOpsConfiguration> _settings;

    private static DevOpsConfiguration? _overrideSettings = null;

    public DevOpsConfiguration GetSettings() => _overrideSettings ?? _settings.Value;

    public void OverrideSettings(DevOpsConfiguration settings)
    {
        _overrideSettings = settings;
    }

    public DevOpsConfigurationAccessor(IOptionsSnapshot<DevOpsConfiguration> settings)
    {
        _settings = settings;
    }


}