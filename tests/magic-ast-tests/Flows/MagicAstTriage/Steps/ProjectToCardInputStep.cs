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
/// Filters out cards with no oracle text AND no faces — those have nothing for
/// the parser to chew on. Multi-faced layouts (split/transform/flip) project
/// their faces into <see cref="CardInputDTO.CardFaces"/>.
/// </remarks>
[FlowthruStep]
public static class ProjectToCardInputStep
{
  public static Func<IEnumerable<MastRawScryfallCard>, IEnumerable<MastCardInput>> Create() =>
    cards =>
      cards
        .Where(card =>
          !string.IsNullOrWhiteSpace(card.OracleText) || (card.CardFaces?.Count ?? 0) > 0
        )
        .Select(card => new MastCardInput
        {
          ScryfallId = card.Id,
          Input = ToInputDTO(card),
        })
        .ToList();

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
