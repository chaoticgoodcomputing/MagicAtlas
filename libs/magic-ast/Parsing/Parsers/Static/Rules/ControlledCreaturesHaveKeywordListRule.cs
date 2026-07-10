namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses "Creatures you control have [keyword1] and [keyword2]." (True
/// Conviction: "Creatures you control have double strike and lifelink.") — a
/// static continuous effect (CR 611.3: "A continuous effect may be generated
/// by the static ability of an object. Example: A permanent with the static
/// ability 'All white creatures get +1/+1' generates an effect that
/// continuously gives +1/+1 to each white creature on the battlefield.")
/// granting two keyword abilities to every creature the controller controls
/// (CR 702.4 Double strike / CR 702.15 Lifelink are both static abilities).
///
/// <para>
/// Sibling to <see cref="CreatureTokensHaveKeywordListRule"/> (which owns the
/// "Creature tokens you control have [kw1] and [kw2]." shape, requiring the
/// literal "tokens" noun) and <see cref="SubtypeCreaturesHaveKeywordListRule"/>
/// (which owns "&lt;Subtype&gt; creatures you control have …", requiring a
/// capitalised subtype qualifier immediately before "creatures"). Neither
/// sibling's anchored pattern matches the bare, unqualified "Creatures you
/// control have …" subject (no subtype word, no "tokens" noun), so this rule
/// is a new, disjoint sibling rather than an edit to either shared rule body.
/// </para>
///
/// <para>
/// <see cref="BareKeywordGrantRule"/>'s Arm 2 already owns the single-keyword
/// bare "Creatures you control have [kw]." shape via its
/// <c>BuildBareGrantFilterTarget</c> bare-card-type branch (Combine
/// Chrysalis), but its trailing keyword group only captures ONE keyword — a
/// second " and [kw2]" clause becomes part of the (unrecognised) keyword text
/// and the whole match is declined. This rule closes that specific
/// two-keyword gap for the bare "Creatures" noun, mirroring
/// <see cref="CreatureTokensHaveKeywordListRule"/>'s mandatory-"and" design so
/// it never competes with Arm 2's single-keyword shape.
/// </para>
///
/// <para>
/// Per the MAST multi-effect-per-clause doctrine, the two-keyword grant is
/// bundled into a single <see cref="StaticAbility"/> whose
/// <see cref="Ability.Effects"/> list carries two independent
/// <see cref="GainAbilityEffect"/> nodes — mirroring
/// <see cref="ControlledFilterHaveKeywordListRule"/>'s and
/// <see cref="CreatureTokensHaveKeywordListRule"/>'s two-keyword grant shape.
/// The granted subjects are ordinary (non-token) creatures, so the built
/// filter is <c>CardTypes=["creature"], Controller=You</c> — no
/// <c>IsToken</c> and no <c>Subtypes</c>.
/// </para>
///
/// <para>
/// Anchored (^…$) pattern; the mandatory literal "Creatures"/"Creature" noun
/// as the very first word keeps this rule from matching any sibling clause
/// shape ("Other creatures …" — owned by <see cref="BareKeywordGrantRule"/>
/// Arm 3; "Nontoken creatures …" — owned by
/// <see cref="NontokenCreaturesHaveKeywordRule"/>; "Attacking creatures …" —
/// owned by <see cref="AttackingObjectsHaveKeywordRule"/>; "&lt;Subtype&gt;
/// creatures …" — owned by <see cref="SubtypeCreaturesHaveKeywordListRule"/>;
/// "Creature tokens …" — owned by
/// <see cref="CreatureTokensHaveKeywordListRule"/>), since none of those
/// shapes begin with the literal word "Creature"/"Creatures" immediately
/// followed by "you control have".
/// </para>
/// </summary>
[StaticRule(Priority = 974)]
public sealed class ControlledCreaturesHaveKeywordListRule : IStaticRule
{
  // "Creatures you control have <kw1> and <kw2>." — kw2 mandatory (the " and "
  // conjunction) distinguishes this from the single-keyword shape
  // BareKeywordGrantRule Arm 2 already owns.
  private static readonly Regex _pattern = new(
    @"^\s*Creatures?\s+you\s+control\s+have\s+" +
    @"(?<kw1>[a-z][a-z]*(?:\s+[a-z]+)*?)\s+and\s+(?<kw2>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var kw1 = match.Groups["kw1"].Value.Trim().ToLowerInvariant();
    var granted1 = StaticRuleHelpers.MapKeywordToStaticAbility(kw1);
    if (granted1 is null)
    {
      return null;
    }

    var kw2 = match.Groups["kw2"].Value.Trim().ToLowerInvariant();
    var granted2 = StaticRuleHelpers.MapKeywordToStaticAbility(kw2);
    if (granted2 is null)
    {
      // First keyword resolved but the second didn't — decline entirely so the
      // fallback surfaces the gap rather than emitting a partial grant.
      return null;
    }

    var filter = new ObjectFilter
    {
      CardTypes = ["creature"],
      Controller = ControllerFilter.You,
    };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new GainAbilityEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.Each, Filter = filter },
            GainedAbility = granted1,
          },
          new GainAbilityEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.Each, Filter = filter },
            GainedAbility = granted2,
          },
        ],
      },
    ];
  }
}
