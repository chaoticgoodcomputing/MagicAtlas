namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

[StaticRule(Priority = 974)]
public sealed class SelfPTForEachRule : IStaticRule
{
  // "This creature gets +N/+M for each <filter> you control."
  // Captures the sign and digit for each side, and the complete filter
  // phrase (including the trailing "you control") between "for each" and
  // the terminal period.
  private static readonly Regex _selfPTForEachPattern = new(
    @"^\s*This\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+for\s+each\s+(?<filter>.+?\s+you\s+control)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "This creature gets +N/+M for each <counterType> counter on it."
  // Covers oil-counter scaling and any other named-counter-on-self shape.
  // CountOf captures the full phrase after "for each" — e.g. "oil counter on it".
  private static readonly Regex _selfPTForEachCounterOnItPattern = new(
    @"^\s*This\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+for\s+each\s+(?<filter>\S+\s+counter\s+on\s+it)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Try the "you control" shape first (most common).
    var match = _selfPTForEachPattern.Match(clause.RawText);

    // Fall back to the "counter on it" shape.
    if (!match.Success)
    {
      match = _selfPTForEachCounterOnItPattern.Match(clause.RawText);
    }

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

    // Only handle multiplier-1 increments. Multiplier > 1 is a different
    // family shape and should fall through to the fallback parser.
    if (Math.Abs(power) > 1 || Math.Abs(toughness) > 1)
    {
      return null;
    }

    // The oracle fragment after "for each" and before the period is the
    // count noun phrase. It is EITHER a counter count ("oil counter on it",
    // CR-no-object → CounterCountQuantity) or an object count ("legendary
    // creature you control" → CountQuantity over an ObjectFilter).
    var filterPhrase = match.Groups["filter"].Value.Trim();

    MagicAST.AST.Quantities.Quantity? countQuantity = BuildCountQuantity(filterPhrase);
    if (countQuantity is null)
    {
      // Phrase did not classify into a structured count — defer to fallback
      // rather than emit a stringly-typed count (no free-text counts).
      return null;
    }

    MagicAST.AST.Quantities.Quantity powerModifier = power == 0
      ? MagicAST.AST.Quantities.LiteralQuantity.Of(0)
      : countQuantity;

    MagicAST.AST.Quantities.Quantity toughnessModifier = toughness == 0
      ? MagicAST.AST.Quantities.LiteralQuantity.Of(0)
      : countQuantity;

    return
    [
      new StaticAbility
      {
        Effects = [new ModifyPTEffect
        {
          Target = ObjectReference.Self(),
          PowerModifier = powerModifier,
          ToughnessModifier = toughnessModifier,
        }],
      },
    ];
  }

  // "<counterType> counter on it" — counts of counters, which an ObjectFilter
  // cannot express (a counter is not an object); these become a
  // CounterCountQuantity over Self. Everything else is an object count and is
  // delegated to the shared ObjectFilter phrase parser.
  private static readonly Regex _counterOnItFilter = new(
    @"^(?<type>\S+)\s+counter\s+on\s+it$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static MagicAST.AST.Quantities.Quantity? BuildCountQuantity(string phrase)
  {
    var counterMatch = _counterOnItFilter.Match(phrase);
    if (counterMatch.Success)
    {
      return new MagicAST.AST.Quantities.CounterCountQuantity
      {
        CounterType = counterMatch.Groups["type"].Value.ToLowerInvariant(),
        On = ObjectReference.Self(),
      };
    }

    var filter = StaticRuleHelpers.BuildObjectCountFilter(phrase);
    return filter is null
      ? null
      : new MagicAST.AST.Quantities.CountQuantity { CountOf = filter };
  }
}
