namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;
using MagicAST.Parsing;

/// <summary>
/// "This spell costs [cost] more to cast for each target beyond the first." — the
/// Strive pattern (Kiora's Dismissal, Kiora's Follower's cycle mates, etc.). Strive
/// is an ability word (CR 207.2c: ability words "have no special rules meaning and no
/// individual entries in the Comprehensive Rules"; the "Strive —" prefix is decorative
/// and carries no semantics of its own), so the operative rules text is the plain
/// static sentence this rule matches — the "Strive —" lead-in is stripped as prose,
/// mirroring how ability-word preambles (Landfall, Raid, …) are dropped elsewhere.
///
/// <para>
/// CR 601.2f: "The total cost is the mana cost or alternative cost … plus all
/// additional costs and cost increases, and minus all cost reductions." The tax is
/// self-referential (the spell taxes only itself as it's cast, unlike the
/// permanent-taxes-other-spells shapes on the sibling <c>CostIncreaseEffect</c>
/// rules), so no <c>AffectedObjects</c>/<c>TargetedObject</c>/<c>CasterFilter</c> is
/// set — <see cref="MagicAST.AST.Effects.Resource.CostIncreaseEffect.PerTargetBeyondFirst"/>
/// carries the "for each target beyond the first" scaling instead.
/// </para>
///
/// <para>
/// The extra cost is generalized via <see cref="ManaCostParser"/> (any combination of
/// generic and colored symbols — {U}, {G}, {1}{U}, {2}{B}, …): the generic component
/// sums into <c>Amount</c> and any non-generic symbols are carried verbatim in
/// <c>ManaSymbols</c>, mirroring the split <see cref="ColoredSpellCostIncreaseRule"/>
/// already uses so colored mana is never flattened to generic (CR 601.2's Altar's
/// Reap example distinguishes {B} from {1}).
/// </para>
///
/// <para>
/// Anchored (^…$) to prevent false-positive substring matches inside longer oracle
/// lines. Target selection itself is not modeled (out of scope per the family brief).
/// </para>
/// </summary>
[StaticRule]
public sealed class StrivePerTargetCostIncreaseRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var parsedCost = new ManaCostParser().Parse(match.Groups["cost"].Value);
    if (parsedCost.Symbols.Count == 0)
    {
      return null;
    }

    var genericAmount = 0;
    List<ManaSymbol>? otherSymbols = null;
    foreach (var symbol in parsedCost.Symbols)
    {
      if (symbol.Kind == ManaSymbolKind.Generic)
      {
        genericAmount += symbol.GenericAmount ?? 0;
      }
      else
      {
        (otherSymbols ??= []).Add(symbol);
      }
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Resource.CostIncreaseEffect
        {
          Amount = LiteralQuantity.Of(genericAmount),
          ManaSymbols = otherSymbols,
          PerTargetBeyondFirst = true,
        }],
      },
    ];
  }

  // "[Strive —] This spell costs {cost} more to cast for each target beyond the first."
  // The optional leading ability word ("Strive — ") is decorative (CR 207.2c) and
  // stripped as prose; the em dash and trailing period are both optional to tolerate
  // minor formatting variance. The cost is the raw run of one or more {..} symbols.
  private static readonly Regex _pattern = new(
    @"^\s*(?:Strive\s*[—-]\s*)?This\s+spell\s+costs\s+(?<cost>(?:\{[^}]+\})+)\s+more\s+to\s+cast\s+for\s+each\s+target\s+beyond\s+the\s+first\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );
}
