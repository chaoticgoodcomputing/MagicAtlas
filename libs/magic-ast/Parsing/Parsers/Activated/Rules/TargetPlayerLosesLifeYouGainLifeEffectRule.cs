namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target player loses N life and you gain N life." — the targeted-drain
/// activated-ability shape (Bloodrite Invoker: "{8}: Target player loses 3 life
/// and you gain 3 life.").
///
/// <para>
/// A single "… and …"-joined sentence that expands to two sibling effects:
/// <see cref="LoseLifeEffect"/> (a targeted player loses N) and
/// <see cref="GainLifeEffect"/> (you gain N). This is the activated-ability
/// counterpart of <see cref="MagicAST.Parsing.Parsers.Spell.Rules.TargetPlayerLosesLifeYouGainRule"/>
/// (the instant/sorcery version, e.g. Absorb Vis) and a sibling of
/// <see cref="EachOpponentLosesLifeYouGainLifeEffectRule"/> (the "each opponent"
/// variant, e.g. Vampire Opportunist). The lose-life clause targets a chosen
/// player (<see cref="ObjectReferenceKind.Target"/> over an
/// <see cref="ObjectFilter.Player()"/> filter); the gain-life clause credits the
/// controller (<see cref="ObjectReferenceKind.You"/>).
/// </para>
///
/// <para>
/// CR 119.3: "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly." Both clauses are governed by
/// this rule. The two amounts are parsed independently and need not be equal;
/// the conjunction "and you gain N life" is what identifies the shape.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): "Target player loses N life and you gain N life" is not
/// a substring of any sibling activated rule, and the anchor prevents a future
/// broader pattern from consuming this sentence and silently dropping either
/// conjunct.
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the two effects sit as
/// a flat sibling pair on <c>Effects</c> — not wrapped in a <c>CompositeEffect</c>.
/// <see cref="TryMatch"/> always returns null so the single-effect path never claims
/// the sentence.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 952)]
public sealed class TargetPlayerLosesLifeYouGainLifeEffectRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+";

  // Anchored: "Target player loses N life and you gain N life"
  private static readonly Regex Pattern = new(
    $@"^Target\s+player\s+loses?\s+(?<lose>{CountTokens})\s+life\s+and\s+you\s+gain\s+(?<gain>{CountTokens})\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>Always returns null — this shape always produces two sibling effects.</remarks>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var match = Pattern.Match(effectText.Trim().TrimEnd('.').Trim());
    if (!match.Success)
    {
      return false;
    }

    var loseCount = ActivatedRuleHelpers.ParseNumberWord(match.Groups["lose"].Value) ?? 1;
    var gainCount = ActivatedRuleHelpers.ParseNumberWord(match.Groups["gain"].Value) ?? 1;

    effects = new List<Effect>
    {
      new LoseLifeEffect
      {
        Amount = LiteralQuantity.Of(loseCount),
        Player = ObjectReference.Target(ObjectFilter.Player()),
      },
      new GainLifeEffect
      {
        Amount = LiteralQuantity.Of(gainCount),
        Player = ObjectReference.You(),
      },
    };
    return true;
  }
}
