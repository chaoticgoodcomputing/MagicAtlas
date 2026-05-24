using Flowthru.Step;
using MagicAST;
using MagicAtlas.Ast.Tests.Data._01_Raw.Schemas;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Steps;

/// <summary>
/// Projects raw Scryfall card rows into <see cref="MastCardInput"/> records —
/// pairs each card's Scryfall id with a <see cref="CardInputDTO"/> ready for
/// the MagicAST parser. Pure transform; no I/O.
/// </summary>
/// <remarks>
/// Applies the corpus scope filters (commander-legal + paper printings) AND
/// the parser-input precondition (oracle text or faces must exist). Multi-faced
/// layouts (split/transform/flip) project their faces into
/// <see cref="CardInputDTO.CardFaces"/>.
/// </remarks>
[FlowthruStep]
public static class ProjectToCardInputStep
{
  public static Func<IEnumerable<MastRawScryfallCard>, IEnumerable<MastCardInput>> Create() =>
    cards =>
      cards
        .Where(IsCommanderLegalPaper)
        .Where(card =>
          !string.IsNullOrWhiteSpace(card.OracleText) || (card.CardFaces?.Count ?? 0) > 0
        )
        .Select(card => new MastCardInput
        {
          ScryfallId = card.Id,
          Input = ToInputDTO(card),
        })
        .ToList();

  /// <summary>
  /// Corpus scope filter: commander-legal AND printed in paper. Drops digital-only
  /// printings (Conjure, Arena-only mechanics), un-legal/banned cards, and silver-
  /// bordered / acorn cards. The atlas effort targets the paper-legal Commander
  /// pool — anything outside it adds noise without contributing to the
  /// hand-parsing surface we care about.
  /// </summary>
  private static bool IsCommanderLegalPaper(MastRawScryfallCard card)
  {
    if (card.Legalities is null || !card.Legalities.TryGetValue("commander", out var legality))
    {
      return false;
    }
    if (!string.Equals(legality, "legal", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }
    if (card.Games is null || !card.Games.Contains("paper", StringComparer.OrdinalIgnoreCase))
    {
      return false;
    }
    return true;
  }

  private static CardInputDTO ToInputDTO(MastRawScryfallCard card) =>
    new()
    {
      Id = card.Id,
      Name = card.Name,
      ManaCost = card.ManaCost,
      TypeLine = card.TypeLine,
      OracleText = card.OracleText,
      Power = card.Power,
      Toughness = card.Toughness,
      Loyalty = card.Loyalty,
      Colors = card.Colors?.AsReadOnly(),
      ColorIdentity = card.ColorIdentity?.AsReadOnly(),
      Keywords = card.Keywords?.AsReadOnly(),
      Layout = card.Layout,
      CardFaces = card.CardFaces?.Select(ToFaceDTO).ToList().AsReadOnly(),
    };

  private static CardFaceDTO ToFaceDTO(MastRawScryfallCardFace face) =>
    new()
    {
      Name = face.Name,
      ManaCost = face.ManaCost,
      TypeLine = face.TypeLine,
      OracleText = face.OracleText,
      Power = face.Power,
      Toughness = face.Toughness,
      Loyalty = face.Loyalty,
      Colors = face.Colors?.AsReadOnly(),
    };
}
