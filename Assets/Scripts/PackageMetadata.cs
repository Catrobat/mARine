using System;

[Serializable]
public class PackageMetadata
{
    public string PackageId;

    public string PackageName;

    public bool Mandatory;

    public long Version;

    public string CatalogUrl;

    public string CatalogHashUrl;

    public string SettingsUrl;
}