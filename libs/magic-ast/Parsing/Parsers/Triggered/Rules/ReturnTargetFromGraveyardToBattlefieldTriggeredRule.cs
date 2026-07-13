namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "return target [filter] card from your graveyard to the battlefield [tapped]"
/// — a single-target, filtered reanimation triggered effect (Undergrowth Recon:
/// "At the beginning of your upkeep, return target land card from your graveyard
/// to the battlefield tapped.").
///
/// Parallel to the spell-side <see cref="Spell.Rules.ReturnGraveyardToBattlefieldRule"/>
/// (same filter vocabulary and zone/controller shape), split into a separate file
/// because <see cref="ITriggeredRule"/> and <see cref="Spell.ISpellRule"/> are
/// distinct interfaces, and extended with the optional trailing "tapped" entering
/// status (CR 110.5b) that the spell-side sentence never carries.
///
/// Distinct from <see cref="ReturnAllFromGraveyardToBattlefieldTriggeredRule"/>
/// ("return ALL … cards" — an <c>Each</c> reference, no targeting) and from
/// <see cref="ReturnSelfFromGraveyardToBattlefieldRule"/> ("return THIS card" —
/// a <c>Self</c> reference): this rule is the single-target ("return TARGET …
/// card") counterpart, fully anchored (^…$) so it cannot match either sibling.
///
/// CR 400.7 (an object that moves from one zone to another becomes a new object);
/// CR 404.1 (graveyard); CR 115.1 (target); CR 110.5b (entering tapped).
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class ReturnTargetFromGraveyardToBattlefieldTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^return\s+target\s+(?<filter>permanent|creature|artifact|enchantment|land|card|nonland\s+permanent)\s+card\s+from\s+(?:your|the)\s+graveyard\s+to\s+the\s+battlefield(?:\s+(?<tapped>tapped))?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    var filterText = m.Groups["filter"].Value.ToLowerInvariant();
    var cardTypes = filterText switch
    {
      "permanent" or "nonland permanent" => new List<string> { "permanent" },
      "creature" => new List<string> { "creature" },
      "artifact" => new List<string> { "artifact" },
      "enchantment" => new List<string> { "enchantment" },
      "land" => new List<string> { "land" },
      "card" => new List<string> { "card" },
      _ => new List<string> { "card" },
    };
    var characteristics =
      filterText == "nonland permanent" ? new List<string> { "nonland" } : null;

    effect = new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = cardTypes,
          Characteristics = characteristics?.Select(Characteristic.FromLabel).ToList(),
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
      Tapped = m.Groups["tapped"].Success,
    };
    return true;
  }
}
