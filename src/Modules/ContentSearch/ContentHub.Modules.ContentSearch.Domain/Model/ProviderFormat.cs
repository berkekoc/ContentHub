namespace ContentHub.Modules.ContentSearch.Domain.Model;

/// <summary>Sağlayıcının veri biçimi. Yalnızca adaptör bu ayrımla ilgilenir (ACL).</summary>
public enum ProviderFormat
{
    Json = 0,
    Xml = 1,
}
