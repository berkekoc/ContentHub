namespace ContentHub.BuildingBlocks.Domain;

/// <summary>Kimliği olan alan nesnesi. Eşitlik kimlik üzerindendir.</summary>
public abstract class Entity<TId>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    protected Entity(TId id) => Id = id;

    // EF Core materializasyonu için.
    protected Entity()
    {
    }

    public override bool Equals(object? obj)
        => obj is Entity<TId> other
           && other.GetType() == GetType()
           && EqualityComparer<TId>.Default.Equals(other.Id, Id);

    public override int GetHashCode()
        => EqualityComparer<TId>.Default.GetHashCode(Id);
}
