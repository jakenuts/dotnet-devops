namespace devops.Internal;

public class DevOpsConfiguration
{
    // Be sure to use the full collection uri, i.e. http://myserver:8080/tfs/defaultcollection
    public string CollectionUri { get; set; } = string.Empty;

    public string CollectionPAT { get; set; } = string.Empty;
}