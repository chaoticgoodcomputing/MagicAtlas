namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.References;

/// <summary>
/// Parses Vorinclex-style counter-placement replacement effects:
///
/// 1. "If you would put one or more counters on a permanent or player, put twice that many
///    of each of those kinds of counters on that permanent or player instead."
///    — the controller doubles counter placement on any target (permanent or player).
///    Actor = you (Controller: {Kind:You}); Modifier = double.
///    CR 614 (replacement effects).
///
/// 2. "If an opponent would put one or more counters on a permanent or player, they put half
///    that many of each of those kinds of counters on that permanent or player instead, rounded down."
///    — opponents halve counter placement on any target, rounded down.
///    Actor = opponent (Controller: {Kind:Opponent}); Modifier = halveRoundDown.
///    CR 614 (replacement effects).
///
/// AffectedObjects is null on both (permanent or player = all valid counter recipients;
/// no restriction expressed by the oracle text beyond exhaustive enumeration).
/// </summary>
[StaticRule(Priority = 975)]
public sealed class VorinclexCounterReplacementRule : IStaticRule
{
  // "If you would put one or more counters on a permanent or player, put twice that many
  // of each of those kinds of counters on that permanent or player instead."
  private static readonly Regex _doublePattern = new(
    @"^\s*If\s+you\s+would\s+put\s+one\s+or\s+more\s+counters\s+on\s+a\s+permanent\s+or\s+player,\s+put\s+twice\s+that\s+many\s+of\s+each\s+of\s+those\s+kinds\s+of\s+counters\s+on\s+that\s+permanent\s+or\s+player\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "If an opponent would put one or more counters on a permanent or player, they put half
  // that many of each of those kinds of counters on that permanent or player instead, rounded down."
  private static readonly Regex _halvePattern = new(
    @"^\s*If\s+an\s+opponent\s+would\s+put\s+one\s+or\s+more\s+counters\s+on\s+a\s+permanent\s+or\s+player,\s+they\s+put\s+half\s+that\s+many\s+of\s+each\s+of\s+those\s+kinds\s+of\s+counters\s+on\s+that\s+permanent\s+or\s+player\s+instead,\s+rounded\s+down\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (_doublePattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new ReplacementEffect
            {
              Event = new CounterPlacementEvent
              {
                MinimumQuantity = 1,
                Controller = new ObjectReference { Kind = ObjectReferenceKind.You },
              },
              OriginalEventOccurs = false,
              Modifier = new ReplacementModifier { Type = "double" },
            },
          ],
        },
      ];
    }

    if (_halvePattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects =
          [
            new ReplacementEffect
            {
              Event = new CounterPlacementEvent
              {
                MinimumQuantity = 1,
                Controller = new ObjectReference { Kind = ObjectReferenceKind.Opponent },
              },
              OriginalEventOccurs = false,
              Modifier = new ReplacementModifier { Type = "halveRoundDown" },
            },
          ],
        },
      ];
    }

    return null;
  }
}
