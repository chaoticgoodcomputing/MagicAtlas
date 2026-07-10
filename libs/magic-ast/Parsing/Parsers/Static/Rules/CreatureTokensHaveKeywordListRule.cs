namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses "Creature tokens you control have [keyword1] and [keyword2]." (Aven Wind
/// Guide) — a static continuous effect (CR 604.2: "Static abilities create continuous
/// effects … These effects are active as long as the permanent with the ability
/// remains on the battlefield.") granting two keyword abilities to every creature
/// token the controller controls (CR 702.9 Flying / CR 702.21 Vigilance are both
/// static abilities).
///
/// <para>
/// Sibling to <see cref="ControlledFilterHaveKeywordListRule"/>, which handles the
/// same "[filter] you control have [kw1][ and kw2]" shape for "Other permanents" and
/// "&lt;Subtype&gt; tokens", but deliberately DECLINES the bare "Creature"/"Creatures"
/// noun so it doesn't shadow <see cref="BareKeywordGrantRule"/>'s Arm 2 (which owns
/// the single-keyword "Creature tokens you control have [kw]." shape via its
/// <c>BuildBareGrantFilterTarget</c> bare-card-type branch — Combine Chrysalis).
/// That Arm 2 pattern only captures ONE trailing keyword, so a second " and [kw2]"
/// clause becomes part of the (unrecognised) keyword text and the whole match is
/// declined. This rule closes that specific two-keyword gap for the bare
/// "Creature tokens" noun, without touching either sibling rule's body.
/// </para>
///
/// <para>
/// Per the MAST multi-effect-per-clause doctrine, the two-keyword grant is bundled
/// into a single <see cref="StaticAbility"/> whose <see cref="Ability.Effects"/> list
/// carries two independent <see cref="GainAbilityEffect"/> nodes — mirroring
/// <see cref="BareKeywordPairGrantRule"/>'s enchant/equip dual-keyword shape and
/// <see cref="ControlledFilterHaveKeywordListRule"/>'s filtered-subject two-keyword
/// shape, generalised here to the bare "Creature tokens you control" filter
/// (<c>CardTypes=["creature"], IsToken=true, Controller=You</c> — the same filter
/// shape <see cref="BareKeywordGrantRule"/>'s Arm 2 produces for the single-keyword
/// case, so a card with only one recognised keyword still resolves to an identical
/// AST regardless of which rule wins dispatch).
/// </para>
///
/// <para>
/// The pattern requires the " and " conjunction (kw2 is mandatory, not optional) so
/// this rule strictly targets the two-keyword gap and never competes with
/// <see cref="BareKeywordGrantRule"/>'s Arm 2 over the single-keyword shape it already
/// owns.
/// </para>
/// </summary>
[StaticRule(Priority = 974)]
public sealed class CreatureTokensHaveKeywordListRule : IStaticRule
{
  // "Creature tokens you control have <kw1> and <kw2>." — kw2 mandatory (the " and "
  // conjunction) distinguishes this from the single-keyword shape BareKeywordGrantRule
  // Arm 2 already owns.
  private static readonly Regex _pattern = new(
    @"^\s*Creature\s+tokens\s+you\s+control\s+have\s+" +
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
      IsToken = true,
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
