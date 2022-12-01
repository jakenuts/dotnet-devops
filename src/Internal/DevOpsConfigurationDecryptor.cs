using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace devops.Internal;

public class DevOpsConfigurationProtector :
    IPostConfigureOptions<DevOpsConfiguration>
{
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public DevOpsConfigurationProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _dataProtectionProvider = dataProtectionProvider;
    }

    public void PostConfigure(string name, DevOpsConfiguration options)
    {
        if (options.CollectionUri.StartsWith("http"))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.CollectionUri) ||
            string.IsNullOrWhiteSpace(options.CollectionPAT))
        {
            return;
        }

        Decrypt(options);
    }

    private void Decrypt(DevOpsConfiguration config)
    {
        var protector = _dataProtectionProvider.CreateProtector(Constants.SettingsAppName);
        config.CollectionUri = protector.Unprotect(config.CollectionUri);
        config.CollectionPAT = protector.Unprotect(config.CollectionPAT);
    }

    public void Encrypt(DevOpsConfiguration config)
    {
        var protector = _dataProtectionProvider.CreateProtector(Constants.SettingsAppName);
        config.CollectionUri = protector.Protect(config.CollectionUri);
        config.CollectionPAT = protector.Protect(config.CollectionPAT);
    }
}