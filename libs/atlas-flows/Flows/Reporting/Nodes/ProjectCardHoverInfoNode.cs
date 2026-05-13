using Flowthru.Step;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Flows.Reporting.Nodes;

/// <summary>
/// Projects <see cref="CardCoreData"/> to the flat <see cref="CardHoverInfo"/> shape consumable by
/// the Python Plotly step. Collapses Scryfall's split type-line back into a single string,
/// flattens the color-identity enum list to a WUBRG-ordered character code, and converts the
/// non-Arrow <c>decimal Cmc</c> to <c>double</c>.
/// </summary>
[FlowthruStep]
public static class ProjectCardHoverInfoNode
{
  // WUBRG ordering for the flattened color-identity string. Anything outside this set (shouldn't
  // happen in practice for color identity) is dropped.
  private static readonly ManaColor[] s_wubrgOrder =
  {
    ManaColor.White,
    ManaColor.Blue,
    ManaColor.Black,
    ManaColor.Red,
    ManaColor.Green,
  };

  private static readonly IReadOnlyDictionary<ManaColor, char> s_colorCodes = new Dictionary<
    ManaColor,
    char
  >
  {
    [ManaColor.White] = 'W',
    [ManaColor.Blue] = 'U',
    [ManaColor.Black] = 'B',
    [ManaColor.Red] = 'R',
    [ManaColor.Green] = 'G',
  };

  public static Func<
    IEnumerable<CardCoreData>,
    Task<IEnumerable<CardHoverInfo>>
  > Create() =>
    cards =>
      Task.FromResult<IEnumerable<CardHoverInfo>>(cards.Select(Project).ToList());

  private static CardHoverInfo Project(CardCoreData card) =>
    new()
    {
      CardId = card.Id,
      Name = card.Name,
      ManaCost = card.ManaCost,
      Cmc = (double)card.Cmc,
      TypeLine = BuildTypeLine(card.Types, card.Subtypes),
      ColorIdentity = FlattenColorIdentity(card.ColorIdentity),
      Power = card.Power,
      Toughness = card.Toughness,
      OracleText = card.OracleText,
    };

  private static string BuildTypeLine(IReadOnlyList<string> types, IReadOnlyList<string> subtypes)
  {
    var head = string.Join(' ', types);
    if (subtypes.Count == 0) return head;
    return $"{head} — {string.Join(' ', subtypes)}";
  }

  private static string FlattenColorIdentity(IReadOnlyList<ManaColor>? identity)
  {
    if (identity is null || identity.Count == 0) return string.Empty;
    return new string(
      s_wubrgOrder.Where(identity.Contains).Select(c => s_colorCodes[c]).ToArray()
    );
  }
}
