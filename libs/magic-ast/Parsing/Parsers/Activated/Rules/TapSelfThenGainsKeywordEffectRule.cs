namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Tap this creature. It gains [keyword] until end of turn." — the classic
/// self-tapping "sentinel" defensive ability (Drudge Sentinel: "{3}: Tap this
/// creature. It gains indestructible until end of turn."). The pronoun "It" in the
/// second sentence unambiguously refers back to "this creature" (the source
/// permanent tapped by the first sentence), so both effects target
/// <see cref="ObjectReferenceKind.Self"/>.
///
/// <para>
/// Two sibling effects on a single activated ability:
/// <list type="number">
///   <item>a <see cref="TapEffect"/> tapping the source permanent (CR 701.26a: "To
///   tap a permanent, turn it sideways from an upright position.");</item>
///   <item>a <see cref="GainAbilityEffect"/> granting the keyword for the turn — a
///   continuous effect (CR 611.1: "A continuous effect modifies characteristics of
///   objects … for a fixed or indefinite period.") whose granted keyword is itself a
///   static ability (e.g. CR 702.12a: "Indestructible is a static ability.").</item>
/// </list>
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the two effects sit as a
/// flat sibling pair on <c>Effects</c>. <see cref="TryMatch"/> always returns null so
/// the single-effect path never claims this two-sentence shape. Mirrors
/// <see cref="ExileSelfThenReturnToBattlefieldRule"/>.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 948)]
public sealed class TapSelfThenGainsKeywordEffectRule
  : IActivatedEffectRule,
    IMultiActivatedEffectRule
{
  // Anchored end-to-end: the whole (reminder-stripped) effect interior must be
  // exactly "Tap this creature. It gains <keyword> until end of turn". The keyword
  // group captures 1–2 words while a negative lookahead stops it swallowing "until".
  private static readonly Regex Pattern = new(
    @"^Tap this creature\.\s+It gains?\s+(?<kw>[a-z]+(?:\s+(?!until\b)[a-z]+)?)\s+until end of turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>
  /// Always returns null — this shape always produces two sibling effects, so it is
  /// served exclusively via <see cref="TryMatchMulti"/>.
  /// </remarks>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;

    var match = Pattern.Match(effectText.Trim().TrimEnd('.'));
    if (!match.Success)
    {
      return false;
    }

    var keyword = match.Groups["kw"].Value.ToLowerInvariant().Trim();
    var gainedAbility = ActivatedRuleHelpers.BuildGrantedKeywordAbility(keyword);
    if (gainedAbility is null)
    {
      // Unmodeled keyword — leave the interior for the residual path rather than
      // emitting a half-structured grant.
      return false;
    }

    effects = new List<Effect>
    {
      new TapEffect { Target = ObjectReference.Self() },
      new GainAbilityEffect
      {
        Target = ObjectReference.Self(),
        GainedAbility = gainedAbility,
        Duration = UntilTimeDuration.EndOfTurn,
      },
    };
    return true;
  }
}
