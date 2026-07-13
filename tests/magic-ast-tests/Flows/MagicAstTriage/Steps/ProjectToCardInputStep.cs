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
/// Applies the corpus scope filter (commander-legal) AND the parser-input
/// precondition (oracle text or faces must exist). Multi-faced layouts
/// (split/transform/flip) project their faces into
/// <see cref="CardInputDTO.CardFaces"/>.
/// </remarks>
[FlowthruStep]
public static class ProjectToCardInputStep
{
  public static Func<IEnumerable<MastRawScryfallCard>, IEnumerable<MastCardInput>> Create() =>
    cards =>
      cards
        .Where(IsCommanderLegal)
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
  /// Corpus scope filter: commander-legal. This alone drops digital-only cards
  /// (Alchemy / Conjure — all <c>commander: not_legal</c>), un-legal/banned
  /// cards, and silver-bordered / acorn cards. We deliberately do NOT filter on
  /// the <c>games</c> array: it is a <b>per-printing</b> field, and the Scryfall
  /// oracle-cards bulk carries only one representative printing per oracle id, so
  /// an old paper staple whose representative printing happens to be MTGO/Arena
  /// (e.g. Demonic Consultation, Lotus Petal, Lion's Eye Diamond, Strip Mine)
  /// would be spuriously dropped — silently excising cEDH combo pieces from the
  /// entire corpus. Commander-legality is the honest paper-pool signal here.
  /// </summary>
  private static bool IsCommanderLegal(MastRawScryfallCard card)
  {
    if (card.Legalities is null || !card.Legalities.TryGetValue("commander", out var legality))
    {
      return false;
    }
    return string.Equals(legality, "legal", StringComparison.OrdinalIgnoreCase);
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
      ColorIndicator = card.ColorIndicator?.AsReadOnly(),
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
      ColorIndicator = face.ColorIndicator?.AsReadOnly(),
    };
}
