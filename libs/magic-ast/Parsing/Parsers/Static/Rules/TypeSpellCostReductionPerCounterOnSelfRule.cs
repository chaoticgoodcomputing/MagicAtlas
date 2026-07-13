namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "[Type] spells you cast cost {N} less to cast for each [counterType] counter
/// on [self]." — a per-counter cost reduction scaled by counters on the source
/// permanent itself (Animar, Soul of Elements: "Creature spells you cast cost
/// {1} less to cast for each +1/+1 counter on Animar.").
///
/// <para>
/// CR 118.7: "What a player actually needs to do to pay a cost may be changed or
/// reduced by effects." The per-object <see cref="MagicAST.AST.Effects.Resource.CostReductionEffect.PerObject"/>
/// axis (used by the sibling <see cref="CostReductionForEachRule"/>) counts
/// OBJECTS matching an <see cref="MagicAST.AST.References.ObjectFilter"/>; a
/// counter is not an object, so the count
/// here is instead a <see cref="CounterCountQuantity"/> carried directly on
/// <see cref="MagicAST.AST.Effects.Resource.CostReductionEffect.Amount"/> — the
/// same shape <see cref="SelfPTForEachRule"/> uses for "counter on it" on the
/// P/T-modification sibling.
/// </para>
///
/// <para>
/// The subject after "on" is either the pronoun "it"/"this creature" or the
/// card's own name (self-by-name, the legendary self-reference convention seen
/// in <c>SelfNamePTForEachCounterYouHaveRule</c>) — both resolve to
/// <see cref="ObjectReference.Self"/> since a static ability can only ever
/// reference the permanent it's printed on. The subject text is captured for
/// anchoring but not otherwise inspected.
/// </para>
///
/// <para>
/// Reuses <see cref="StaticRuleHelpers.BuildTypeSpellFilter"/> (the same helper
/// backing <see cref="TypeSpellCostReductionRule"/>) for the "[Type] spells you
/// cast" affected-objects filter, so the type-word vocabulary (creature,
/// artifact, colour names, etc.) stays in one place. Anchored (^…$) so the
/// "for each … counter on …" suffix can't be swallowed by a sibling
/// cost-reduction pattern that lacks it.
/// </para>
/// </summary>
[StaticRule(Priority = 982)]
public sealed class TypeSpellCostReductionPerCounterOnSelfRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var filterText = match.Groups["filter"].Value.Trim();
    var amount = int.Parse(match.Groups["amount"].Value);
    var counterType = match.Groups["counterType"].Value.ToLowerInvariant();

    var affected = StaticRuleHelpers.BuildTypeSpellFilter(filterText);
    if (affected is null)
    {
      return null;
    }

    var count = new CounterCountQuantity
    {
      CounterType = counterType,
      On = ObjectReference.Self(),
    };

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Resource.CostReductionEffect
        {
          Amount = BuildAmount(amount, count),
        }],
        AffectedObjects = affected,
      },
    ];
  }

  // The reduction amount = (per-counter increment) × (counter count). A bare
  // {1} reduction reuses the count directly (mirrors SelfPTForEachRule's
  // single-increment convention); any other magnitude wraps it in a multiply
  // CalculatedQuantity.
  private static Quantity BuildAmount(int increment, Quantity count)
  {
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

  // "Creature spells you cast cost {1} less to cast for each +1/+1 counter on Animar."
  // The subject after "on" is either a pronoun ("it"/"this creature") or a
  // capitalised card name; both resolve to Self, so it is captured but not
  // further inspected.
  private static readonly Regex _pattern = new(
    @"^\s*(?<filter>[A-Z][A-Za-z]+)\s+spells\s+you\s+cast\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\s+for\s+each\s+(?<counterType>\+1/\+1|-1/-1|[\w\-]+)\s+counter\s+on\s+(?<subject>it|this\s+creature|[A-Z][A-Za-z'\-]+(?:,\s+[A-Za-z'\-]+)*(?:\s+[A-Za-z'\-]+)*)\.?\s*$",
    RegexOptions.Compiled
  );
}
