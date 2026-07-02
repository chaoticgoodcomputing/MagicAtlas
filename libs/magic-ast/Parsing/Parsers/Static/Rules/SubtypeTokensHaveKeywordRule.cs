namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Parses "&lt;Subtype&gt; tokens you control have [keyword]." — a static continuous
/// effect (CR 604.1: "Static abilities do something all the time rather than being
/// activated or triggered. They are written as statements, and they're simply
/// true."; CR 604.2: static abilities create continuous effects that remain active
/// as long as the source remains on the battlefield with the ability) that grants a
/// keyword ability to the token-restricted subset of a named creature subtype the
/// controller controls. This is the Eternal Skylord shape ("Zombie tokens you
/// control have flying.").
///
/// <para>
/// The "tokens" qualifier maps to <c>IsToken = true</c> on the
/// <see cref="ObjectFilter"/> (CR 111.1: "A token is a marker used to represent any
/// permanent that isn't represented by a card."). The capitalised subtype word
/// populates <c>Subtypes</c> — no <c>CardTypes</c> is emitted, matching the bare
/// plural-subtype filter convention used elsewhere in this family.
/// </para>
///
/// <para>
/// GUARD: if the captured word is "Creature"/"Creatures" (case-insensitive), this
/// rule declines — that card-type token grant (e.g. Combine Chrysalis) stays owned
/// by <see cref="BareKeywordGrantRule"/> Arm 2, which already handles it via
/// <c>BuildBareGrantFilterTarget</c>'s bare card-type branch.
/// </para>
///
/// <para>
/// Priority 970 — above <see cref="BareKeywordGrantRule"/> (967) and
/// <see cref="NontokenCreaturesHaveKeywordRule"/> (969) so this more-specific
/// "&lt;Subtype&gt; tokens you control have &lt;keyword&gt;" shape fires first and
/// documents precedence over the broader filter arm (functionally order-independent
/// here since BareKeywordGrantRule's Arm 2 bare-subtype branch is guarded
/// against token filters and declines on its own). Anchored pattern prevents
/// substring matches against sibling clauses.
/// </para>
/// </summary>
[StaticRule(Priority = 970)]
public sealed class SubtypeTokensHaveKeywordRule : IStaticRule
{
  // "<Subtype> tokens you control have <keyword>." Anchored (^ ... $) to prevent
  // substring matches. The subtype must be a capitalised word (e.g. "Zombie").
  private static readonly Regex _pattern = new(
    @"^\s*(?<sub>[A-Z][a-z]+)\s+tokens?\s+you\s+control\s+have\s+(?<kw>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);

    var m = _pattern.Match(rawText);
    if (!m.Success)
    {
      return null;
    }

    var sub = m.Groups["sub"].Value.Trim();
    if (sub.Equals("Creature", StringComparison.OrdinalIgnoreCase) ||
        sub.Equals("Creatures", StringComparison.OrdinalIgnoreCase))
    {
      // Card-type token grant — owned by BareKeywordGrantRule Arm 2.
      return null;
    }

    var kw = m.Groups["kw"].Value.Trim().ToLowerInvariant();
    var grantedAbility = StaticRuleHelpers.MapKeywordToStaticAbility(kw);
    if (grantedAbility is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new GainAbilityEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter
            {
              Subtypes = [sub],
              IsToken = true,
              Controller = ControllerFilter.You,
            },
          },
          GainedAbility = grantedAbility,
        }],
      },
    ];
  }
}
