using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._02_Intermediate.Schemas;

/// <summary>
/// Reference to a related Magic card (token, meld part, combo piece).
/// </summary>
public record RelatedCard : IStructuredSerializable
{
  public Guid Id { get; init; }
  public RelatedCardComponent Component { get; init; }
  public string Name { get; init; } = "";
  public string TypeLine { get; init; } = "";
  public string Uri { get; init; } = "";
}
