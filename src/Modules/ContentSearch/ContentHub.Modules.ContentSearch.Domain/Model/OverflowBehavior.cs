namespace ContentHub.Modules.ContentSearch.Domain.Model;

/// <summary>İstek limiti aşıldığında sağlayıcı çağrısının davranışı (Norms 11).</summary>
public enum OverflowBehavior
{
    Wait = 0,
    Retry = 1,
    Break = 2,
}
