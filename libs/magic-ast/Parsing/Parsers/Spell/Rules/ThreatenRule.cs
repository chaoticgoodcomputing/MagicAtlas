namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Recognises the Threaten / Act of Treason pattern:
///   "Gain control of target creature until end of turn. Untap that creature.
///    It gains haste until end of turn."
///
/// Three sequential effects share one target (the chosen creature), represented
/// in the AST as:
/// <list type="bullet">
///   <item><see cref="GainControlEffect"/> — target creature, until end of turn.</item>
///   <item><see cref="UntapEffect"/> — It (back-reference to the same creature).</item>
///   <item><see cref="GainAbilityEffect"/> — It gains haste until end of turn.</item>
/// </list>
///
/// "That creature" in the second sentence is a back-reference to the target
/// chosen for the first sentence; it maps to <see cref="ObjectReferenceKind.It"/>
/// (same pronoun semantics as "it" — Rule 109.2).
///
/// Rule citations: CR 613.1a (Layer 2 — control-changing effects),
/// CR 613.1c (Layer 6 — ability-granting effects), CR 702.10 (haste).
///
/// <para>
/// The single-effect <see cref="ISpellRule.TryMatch"/> always returns false so the
/// flat-list path is the only active route.
/// </para>
/// </summary>
[SpellRule]
public sealed class ThreatenRule : ISpellRule, IMultiSpellRule
{
  // Matches the canonical three-sentence Threaten / Act of Treason oracle text.
  // The sentences are joined by ". " in the trimmed-period input the parser supplies.
  // Capturing "creature" is optional here (only creature targets exist in this shape),
  // but the anchor keeps the match tight so no partial-line false-positive fires.
  private static readonly Regex _pattern = new(
    @"^Gain\s+control\s+of\s+target\s+creature\s+until\s+end\s+of\s+turn\.\s+" +
    @"Untap\s+that\s+creature\.\s+" +
    @"It\s+gains\s+haste\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // -------------------------------------------------------------------------
  // ISpellRule — single-effect path intentionally disabled.
  // -------------------------------------------------------------------------
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  // -------------------------------------------------------------------------
  // IMultiSpellRule — flat effect list: GainControl + Untap + GainAbility(haste).
  // -------------------------------------------------------------------------
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    var duration = UntilTimeDuration.EndOfTurn;
    var targetCreature = new ObjectReference
    {
      Kind = ObjectReferenceKind.Target,
      Filter = new ObjectFilter { CardTypes = ["creature"] },
    };
    var it = new ObjectReference { Kind = ObjectReferenceKind.It };

    effects = new List<Effect>
    {
      // Sentence 1: "Gain control of target creature until end of turn."
      new GainControlEffect
      {
        Target = targetCreature,
        Duration = duration,
      },

      // Sentence 2: "Untap that creature."  — "that creature" = It (back-reference).
      new UntapEffect
      {
        Target = it,
      },

      // Sentence 3: "It gains haste until end of turn."
      new GainAbilityEffect
      {
        Target = it,
        GainedAbility = new StaticAbility
        {
          KeywordSource = "Haste",
          Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Haste }],
        },
        Duration = duration,
      },
    };
    return true;
  }
}
