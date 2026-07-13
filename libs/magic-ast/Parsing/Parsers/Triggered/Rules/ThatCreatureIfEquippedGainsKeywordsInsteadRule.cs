namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Static;

/// <summary>
/// "If that creature is equipped, it gains &lt;kw1&gt; and &lt;kw2&gt; until end
/// of turn instead." — the equipped-override sentence of Éowyn, Lady of Rohan,
/// paired with
/// <see cref="TargetCreatureGainsChoiceOfKeywordsUntilEndOfTurnRule"/> (the
/// preceding "your choice of …" default). When the previously-granted target
/// creature ("that creature" / "it") is currently equipped, it instead gains
/// BOTH named keywords for the turn.
///
/// <para>
/// Modelled as a <see cref="ConditionalEffect"/> gated on the new
/// <see cref="ObjectIsEquippedCondition"/> (CR 702.6 — a creature with an
/// Equipment attached is "equipped"), whose <see cref="ConditionalEffect.Then"/>
/// is a <see cref="CompositeEffect"/> of one <see cref="GainAbilityEffect"/> per
/// keyword, each targeting the anaphoric "it"
/// (<see cref="ObjectReferenceKind.It"/>) with an "until end of turn" duration
/// (CR 611.1). "and" (not "or") means both keywords are granted — a
/// <see cref="CompositeEffect"/>, not a <see cref="ModalEffect"/>. The word
/// "instead" is the replacement relationship the preceding default sentence's
/// grant has when this condition holds; the two sentences are decomposed as two
/// sibling effects by the triggered sentence-bundle splitter (functionally the
/// union of the grants equals "both", so the flat decomposition preserves the
/// end state).
/// </para>
///
/// <para>
/// Anchored (^…$); the literal "If that creature is equipped … instead" lead-in
/// and tail make it disjoint from every other triggered effect shape.
/// </para>
/// </summary>
[TriggeredRule(Priority = 65)]
public sealed class ThatCreatureIfEquippedGainsKeywordsInsteadRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^If\s+that\s+creature\s+is\s+equipped,\s+it\s+gains\s+(?<kw1>.+?)\s+and\s+(?<kw2>.+?)\s+until\s+end\s+of\s+turn\s+instead\.?$",
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

    var first = StaticRuleHelpers.MapKeywordToStaticAbility(m.Groups["kw1"].Value.Trim());
    var second = StaticRuleHelpers.MapKeywordToStaticAbility(m.Groups["kw2"].Value.Trim());
    if (first is null || second is null)
    {
      // Unrecognised keyword — bail so fallback handles it; no free text.
      return false;
    }

    effect = new ConditionalEffect
    {
      Condition = new ObjectIsEquippedCondition { Reference = ObjectReference.It() },
      Then = new CompositeEffect
      {
        Effects =
        [
          new GainAbilityEffect
          {
            Target = ObjectReference.It(),
            GainedAbility = first,
            Duration = UntilTimeDuration.EndOfTurn,
          },
          new GainAbilityEffect
          {
            Target = ObjectReference.It(),
            GainedAbility = second,
            Duration = UntilTimeDuration.EndOfTurn,
          },
        ],
      },
    };
    return true;
  }
}
