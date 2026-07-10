namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "return target [filter] card with mana value N or less/greater[/exactly N] from
/// your graveyard to the battlefield [tapped]" — a single-target, filtered
/// reanimation triggered effect whose filter carries a printed mana-value bound
/// (Driver of the Dead: "When this creature dies, return target creature card with
/// mana value 2 or less from your graveyard to the battlefield.").
///
/// The mana-value-bounded sibling of
/// <see cref="ReturnTargetFromGraveyardToBattlefieldTriggeredRule"/>: that rule
/// requires "[filter] card from" with no interposed clause, so it cannot match this
/// "[filter] card WITH mana value …" shape, and this rule requires the "with mana
/// value …" clause, so it cannot match the plain sibling — the two are mutually
/// exclusive and both fully anchored (^…$). The mana-value bound lands on
/// <see cref="ObjectFilter.ManaValueComparison"/> (the existing structured axis;
/// mirrors Kami of Empty Graves' return-to-hand gold), so no new discriminator is
/// introduced — the effect reuses <see cref="ReturnToBattlefieldEffect"/>.
///
/// CR 400.7 (an object that moves from one zone to another becomes a new object);
/// CR 404.1 (a player's graveyard is their discard pile); CR 115.1 (target);
/// CR 603.2 (triggered ability fires on its trigger event — "when this creature dies").
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class ReturnTargetCardWithManaValueFromGraveyardToBattlefieldTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^return\s+target\s+(?<filter>permanent|creature|artifact|enchantment|land|card)\s+card\s+with\s+mana\s+value\s+(?<mv>\d+)(?:\s+or\s+(?<bound>less|greater))?\s+from\s+(?:your|the)\s+graveyard\s+to\s+the\s+battlefield(?:\s+(?<tapped>tapped))?$",
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
      "permanent" => new List<string> { "permanent" },
      "creature" => new List<string> { "creature" },
      "artifact" => new List<string> { "artifact" },
      "enchantment" => new List<string> { "enchantment" },
      "land" => new List<string> { "land" },
      _ => new List<string> { "card" },
    };

    var manaValue = int.Parse(m.Groups["mv"].Value);
    var op = m.Groups["bound"].Value.ToLowerInvariant() switch
    {
      "less" => ComparisonOperator.LessThanOrEqual,
      "greater" => ComparisonOperator.GreaterThanOrEqual,
      _ => ComparisonOperator.Equal,
    };

    effect = new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = cardTypes,
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
          ManaValueComparison = new Comparison
          {
            Operator = op,
            Value = manaValue,
          },
        },
      },
      Tapped = m.Groups["tapped"].Success,
    };
    return true;
  }
}
