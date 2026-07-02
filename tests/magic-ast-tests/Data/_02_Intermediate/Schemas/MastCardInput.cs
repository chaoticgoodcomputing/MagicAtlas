using Flowthru.Data.Schema;
using MagicAST;

namespace MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;

/// <summary>
/// One card's input to MagicAST, paired with its Scryfall identity. The
/// <see cref="Input"/> field is MagicAST's own <see cref="CardInputDTO"/> —
/// keeping the projection target stable means the parser library doesn't need
/// a Flowthru dependency, and the triage flow doesn't need a parallel DTO.
/// </summary>
[FlowthruSchema]
public partial record MastCardInput
{
  /// <summary>Scryfall card id — primary key for the source row.</summary>
  public required string ScryfallId { get; init; }

  /// <summary>The DTO consumed by MagicAST's <c>CardParser</c>/<c>OracleParser</c>.</summary>
  public required CardInputDTO Input { get; init; }
}
