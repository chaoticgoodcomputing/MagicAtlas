namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[Name] gets +N/+M for each [counterType] counter you have." — a self-anthem
/// scaled by a player-owned (not permanent-owned) counter count (Kalemne,
/// Disciple of Iroas: "Kalemne gets +1/+1 for each experience counter you
/// have."). CR 611.3 (a static ability that sets or modifies power/toughness);
/// CR 122 (counters — CR 122.1 counters may be placed on a player, not only an
/// object; CR 122.1 experience counters specifically).
///
/// <para>
/// Self-by-name sibling of <see cref="SelfPTForEachRule"/> (which is anchored
/// to "This creature gets…"): legendary creatures frequently name themselves
/// instead, mirroring the <c>SelfName*</c> convention used elsewhere (e.g.
/// <c>SelfNameEntersTappedDoesntUntapRule</c>). Distinct from
/// <see cref="SelfPTForEachRule"/>'s "&lt;counterType&gt; counter on it" phrase
/// (a permanent-owned <see cref="CounterCountQuantity"/> with
/// <see cref="ObjectReference.Self"/>): "counter … you have" is a player-owned
/// counter count, so <see cref="CounterCountQuantity.On"/> is
/// <see cref="ObjectReference.You"/> instead.
/// </para>
///
/// <para>
/// Priority 973 — just below <see cref="SelfPTForEachRule"/> (974); the two
/// patterns are disjoint (one requires the literal "This creature", the other a
/// capitalised card name) so ordering between them is not load-bearing, but the
/// tier mirrors the sibling rule's specificity. Fully anchored (^…$) so it
/// cannot substring-match a differently-shaped sibling clause.
/// </para>
/// </summary>
[StaticRule(Priority = 973)]
public sealed class SelfNamePTForEachCounterYouHaveRule : IStaticRule
{
  // "[CardName] gets +N/+M for each <counterType> counter you have." The name
  // portion mirrors the SelfNameEntersTappedDoesntUntapRule convention: one or
  // more capitalised words, optionally followed by ", <epithet>".
  private static readonly Regex _pattern = new(
    @"^\s*[A-Z][A-Za-z'\-]+(?:,\s+[A-Z][A-Za-z'\-]+)*(?:\s+[A-Za-z'\-]+)*\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+for\s+each\s+(?<type>[\w\-]+)\s+counter\s+you\s+have\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var psign = match.Groups["psign"].Value;
    var p = int.Parse(match.Groups["p"].Value);
    var tsign = match.Groups["tsign"].Value;
    var t = int.Parse(match.Groups["t"].Value);

    var power = psign == "-" ? -p : p;
    var toughness = tsign == "-" ? -t : t;
    var counterType = match.Groups["type"].Value.ToLowerInvariant();

    var count = new CounterCountQuantity
    {
      CounterType = counterType,
      On = ObjectReference.You(),
    };

    return
    [
      new StaticAbility
      {
        Effects = [new ModifyPTEffect
        {
          Target = ObjectReference.Self(),
          PowerModifier = BuildSideModifier(power, count),
          ToughnessModifier = BuildSideModifier(toughness, count),
        }],
      },
    ];
  }

  // Per-side dynamic modifier = (per-each increment) × (count). Mirrors
  // SelfPTForEachRule's BuildSideModifier: a bare +1 increment reuses the count
  // directly; any other magnitude (including negative) wraps it in a signed
  // multiply CalculatedQuantity.
  private static Quantity BuildSideModifier(int increment, Quantity count)
  {
    if (increment == 0)
    {
      return LiteralQuantity.Of(0);
    }

    if (increment == 1)
    {
      return count;
    }

    return new CalculatedQuantity
    {
      BaseQuantity = count,
      Operation = "multiply",
      Operand = increment,
    };
  }
}
