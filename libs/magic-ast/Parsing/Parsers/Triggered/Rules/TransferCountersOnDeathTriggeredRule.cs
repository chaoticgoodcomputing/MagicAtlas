namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "put its counters on target creature you control" — counter-transfer shape
/// that moves all counters from the dying creature to a target controlled
/// creature. The phrase "its counters" is an anaphoric reference to the
/// triggering creature's current counter set (all types and amounts); MAST
/// describes this as <c>CounterType = "all"</c> (the sentinel for "every
/// counter type present on the source") with a <see cref="DerivedQuantity"/>
/// source of "its", meaning the count is whatever is on the dying permanent
/// rather than a fixed number (Rule 122.1 — counters are game objects; their
/// quantity is a game state).
///
/// Oracle text shape (after trigger/effect split):
///   "put its counters on target creature you control"
///
/// Example: "When Goobling dies, put its counters on target creature you
///   control." (ONE)
///
/// Priority 60: more specific than the generic <see cref="PutCountersTriggeredRule"/>
/// (priority 50) which requires an explicit counter type ("a +1/+1 counter",
/// "a charge counter", etc.) and would fall through on "its counters" anyway.
/// The higher priority ensures this rule is tried first for the "its counters"
/// surface without ambiguity.
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class TransferCountersOnDeathTriggeredRule : ITriggeredRule
{
  // Matches: "[you may ]put its counters on target creature you control"
  // The "its counters" phrase is the distinguishing token: no article ("a",
  // "an"), no explicit counter type — just the possessive pronoun "its".
  private static readonly Regex _pattern = new(
    @"^(?<opt>you\s+may\s+)?put\s+its\s+counters\s+on\s+target\s+creature\s+you\s+control$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var match = _pattern.Match(text.Trim());
    if (!match.Success)
    {
      return false;
    }

    var isOptional = match.Groups["opt"].Success;

    effect = new PutCountersEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
        },
      },
      // "all" is the sentinel counter type meaning "every counter type present
      // on the source permanent" — the possessive "its" in oracle text carries
      // this meaning (Rule 122.1; CR 701.11e for the Modular reminder context).
      CounterType = "all",
      // The count is not a fixed literal; it is derived from the source
      // permanent's current counter state at the time the ability resolves.
      // DerivedKind.Other + Source = "its" encodes this reference faithfully
      // without inventing a new enum case for a single-surface pattern.
      Count = new DerivedQuantity
      {
        DerivedFrom = DerivedKind.Other,
        Source = "its",
      },
      IsOptional = isOptional,
    };
    return true;
  }
}
