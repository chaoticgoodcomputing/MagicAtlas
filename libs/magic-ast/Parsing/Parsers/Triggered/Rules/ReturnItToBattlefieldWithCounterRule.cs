namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "return it to the battlefield under its owner's control with a [type] counter on it" —
/// the Luminous Broodmoth / Persist / Undying-style counter-on-return shape where the
/// return and the counter placement are written as a single clause (CR 400.6 / CR 122.1).
///
/// <para>
/// This complements <see cref="ReturnSelfToBattlefieldUnderOwnerControlRule"/> (bare
/// "return it … under its owner's control", no counter suffix). Priority is higher (71)
/// so the more specific counter-bearing form is tried before the bare form.
/// </para>
///
/// <para>
/// The counter is encoded on <see cref="ReturnToBattlefieldEffect.WithCounters"/> — the
/// same field used by the Undying/Persist keyword expansions (UndyingKeyword, PersistKeyword)
/// — so the interaction layer treats it identically. Counter type is lowercased to match the
/// <see cref="MagicAST.AST.Effects.ZoneChange.CounterPlacement.CounterType"/> convention
/// ("flying", "+1/+1", "-1/-1", "time", etc.).
/// </para>
///
/// CR 400.6 (enters under owner's control); CR 113.8b ("it" = the triggering object);
/// CR 122.1 (counters); CR 702.9a (flying counter).
/// </summary>
[TriggeredRule(Priority = 71)]
public sealed class ReturnItToBattlefieldWithCounterRule : ITriggeredRule
{
  // Matches: "return it to the battlefield under its owner's control with a <type> counter on it"
  // Captures <type> — the counter-type word(s) before "counter", e.g. "flying", "+1/+1", "time".
  private static readonly Regex _pattern = new(
    @"^return\s+it\s+to\s+the\s+battlefield\s+under\s+its\s+owner(?:'s|s')\s+control\s+with\s+a(?:n)?\s+(?<type>[\w+/\-]+)\s+counter\s+on\s+it$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var counterType = m.Groups["type"].Value.ToLowerInvariant();

    effect = new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.It },
      UnderControl = new ObjectReference { Kind = ObjectReferenceKind.Owner },
      Tapped = false,
      WithCounters = new CounterPlacement
      {
        CounterType = counterType,
        Count = LiteralQuantity.Of(1),
      },
    };
    return true;
  }
}
