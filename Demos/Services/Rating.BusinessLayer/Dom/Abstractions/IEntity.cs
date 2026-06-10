namespace Arky.Dom.Abstractions;

public interface IEntity
{
    Guid Id { get; }
    bool Validate();
}