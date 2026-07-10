namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 987)]
public sealed class CostReductionForEachRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _costReductionForEachPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var amount = int.Parse(match.Groups["amount"].Value);
    var filterPhrase = match.Groups["filter"].Value.Trim().ToLowerInvariant();

    ObjectFilter? perObject = null;

    // "creature card in your graveyard"
    if (filterPhrase == "creature card in your graveyard")
    {
      perObject = new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = ControllerFilter.You,
        Zone = Zone.Graveyard,
      };
    }
    // "instant and sorcery card in your graveyard"
    else if (filterPhrase == "instant and sorcery card in your graveyard")
    {
      perObject = new ObjectFilter
      {
        CardTypes = ["instant", "sorcery"],
        Controller = ControllerFilter.You,
        Zone = Zone.Graveyard,
      };
    }
    // "creature that attacked this turn"
    else if (filterPhrase == "creature that attacked this turn")
    {
      perObject = new ObjectFilter
      {
        CardTypes = ["creature"],
        History = new MagicAST.AST.References.OtherHistoryPredicate
        {
          Description = "attacked this turn",
        },
      };
    }
    // "creature on the battlefield" — Blasphemous Act's sweeper-discount:
    // CR 400.1 defines the battlefield as the zone where permanents exist;
    // this phrase selects all creature permanents currently in play.
    else if (filterPhrase == "creature on the battlefield")
    {
      perObject = new ObjectFilter
      {
        CardTypes = ["creature"],
        Zone = Zone.Battlefield,
      };
    }
    // "attacking creature" — Stone Idol Trap's per-attacker discount. "Attacking"
    // is a combat-state predicate (CR 508; Glossary "Attacking Creature"), not a
    // creature subtype, so it is encoded via CombatStateCharacteristic rather than
    // Subtypes — mirroring the codebase-wide convention for "attacking creature"
    // filters (e.g. ModifyPTTargetAttackingCreatureEffectRule).
    else if (filterPhrase == "attacking creature")
    {
      perObject = new ObjectFilter
      {
        CardTypes = ["creature"],
        Characteristics = [Characteristic.InCombat(CombatState.Attacking)],
      };
    }

    if (perObject is null)
    {
      // Unrecognised filter phrase — let the fallback record the gap.
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Resource.CostReductionEffect
        {
          Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
          PerObject = perObject,
        }],
      },
    ];
  }

  // "This spell costs {N} less to cast for each <filter>."
  // Captures the generic amount and the filter phrase verbatim (trimmed,
  // without the terminal period).
  private static readonly Regex _costReductionForEachPattern = new(
    @"^\s*This\s+spell\s+costs\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\s+for\s+each\s+(?<filter>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );
}
