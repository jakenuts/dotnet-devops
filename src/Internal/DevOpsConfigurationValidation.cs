using Microsoft.Extensions.Options;

namespace devops.Internal;

public class DevOpsConfigurationValidation : IValidateOptions<DevOpsConfiguration>
{
    public ValidateOptionsResult Validate(string name, DevOpsConfiguration options)
    {
        if (string.IsNullOrEmpty(options.CollectionUri))
            return ValidateOptionsResult.Fail("CollectionUri must be set or updated using dotnet init");

        if (string.IsNullOrEmpty(options.CollectionPAT))
            return ValidateOptionsResult.Fail("CollectionPAT must be set or updated using dotnet init");


        return ValidateOptionsResult.Success;
    }
}