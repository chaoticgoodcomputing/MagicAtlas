namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

[StaticRule(Priority = 974)]
public sealed class SelfPTForEachRule : IStaticRule
{
  // "This creature gets +N/+M for each <count phrase>." The count phrase after
  // "for each" (and before the terminal period) is captured whole and
  // classified downstream into a structured Quantity — a board count
  // ("legendary creature you control"), a counter count ("oil counter on it"),
  // or a derived characteristic count ("card in your hand"). Per-side sign and
  // magnitude are captured separately so a magnitude > 1 (e.g. "-4/-4") becomes
  // a structured multiply, not a free-text expression.
  // CR 613.4c (Layer 7c): a P/T-modifying effect that does not set P/T to a
  // specific value.
  private static readonly Regex _selfPTForEachPattern = new(
    @"^\s*This\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+for\s+each\s+(?<filter>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _selfPTForEachPattern.Match(clause.RawText);
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

    // The oracle fragment after "for each" and before the period is the
    // count noun phrase. It is a counter count ("oil counter on it",
    // CR-no-object → CounterCountQuantity), a derived characteristic count
    // ("card in your hand" → DerivedQuantity{CardsInHand}), or an object count
    // ("legendary creature you control" → CountQuantity over an ObjectFilter).
    var filterPhrase = match.Groups["filter"].Value.Trim();

    MagicAST.AST.Quantities.Quantity? countQuantity = BuildCountQuantity(filterPhrase);
    if (countQuantity is null)
    {
      // Phrase did not classify into a structured count — defer to fallback
      // rather than emit a stringly-typed count (no free-text counts).
      return null;
    }

    var powerModifier = BuildSideModifier(power, countQuantity);
    var toughnessModifier = BuildSideModifier(toughness, countQuantity);

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

  // Per-side dynamic modifier = (per-each increment) × (count). A zero
  // increment is a literal 0; a +1 increment reuses the bare count (the
  // single-increment convention); any other increment — including a negative
  // self-debuff like "-4/-4 for each card in your hand" — wraps the count in a
  // multiply CalculatedQuantity whose signed Operand carries BOTH magnitude and
  // sign as structure (no free-text expression; mirrors Strong Back's
  // "+2/+2 for each Aura" gold). A bare "+1" stays bare; "-1" still needs the
  // wrapper because the negative sign must be modeled.
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

  // "<counterType> counter on it" — counts of counters, which an ObjectFilter
  // cannot express (a counter is not an object); these become a
  // CounterCountQuantity over Self.
  private static readonly Regex _counterOnItFilter = new(
    @"^(?<type>\S+)\s+counter\s+on\s+it$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "card(s) in your hand" — a derived characteristic count (the size of your
  // hand), not an object count over an ObjectFilter. Maps to the existing
  // DerivedKind.CardsInHand (Dread Slag, Maro family). "your hand" is implicit
  // self-ownership, so no Source is recorded — matching the Peek gold.
  private static readonly Regex _cardsInHandFilter = new(
    @"^cards?\s+in\s+your\s+hand$",
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

    if (_cardsInHandFilter.IsMatch(phrase))
    {
      return new MagicAST.AST.Quantities.DerivedQuantity
      {
        DerivedFrom = MagicAST.AST.Quantities.DerivedKind.CardsInHand,
      };
    }

    var filter = StaticRuleHelpers.BuildObjectCountFilter(phrase);
    return filter is null
      ? null
      : new MagicAST.AST.Quantities.CountQuantity { CountOf = filter };
  }
}
