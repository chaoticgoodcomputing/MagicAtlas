namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Each opponent loses N life and you gain N life." — the vampire-drain
/// activated-ability shape (Vampire Opportunist).
///
/// <para>
/// A single "… and …"-joined sentence that expands to two sibling effects:
/// <see cref="LoseLifeEffect"/> (each opponent loses N) and
/// <see cref="GainLifeEffect"/> (you gain N). Differs from the similar
/// <see cref="EachOpponentLoseLifeEffectRule"/> (bare single-clause "Each
/// opponent loses N life" only) and <see cref="GainLifeEffectRule"/> (bare
/// single-clause "You gain N life" only) — neither of those anchored
/// single-effect rules matches this compound sentence.
/// </para>
///
/// <para>
/// CR 119.3 (life gain/loss): "If an effect causes a player to gain life or
/// lose life, that player's life total is adjusted accordingly." Both
/// clauses are governed by this rule; the lose-life clause targets each
/// opponent (<see cref="ObjectReferenceKind.EachOpponent"/>) and the
/// gain-life clause targets the controller (<see cref="ObjectReferenceKind.You"/>).
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): "Each opponent loses N life and you gain N life"
/// appears as a SUBSTRING of no sibling rule, but the anchor prevents a
/// future broader pattern from consuming this sentence and silently
/// dropping either conjunct.
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the two effects
/// sit as a flat sibling pair on <c>Effects</c> — not wrapped in a
/// <c>CompositeEffect</c>. <see cref="TryMatch"/> always returns null so the
/// single-effect path never claims the sentence.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 953)]
public sealed class EachOpponentLosesLifeYouGainLifeEffectRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  // Anchored: "Each opponent loses N life and you gain N life"
  private static readonly Regex Pattern = new(
    @"^Each\s+opponent\s+loses\s+(?<lose>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life\s+and\s+you\s+gain\s+(?<gain>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>Always returns null — this shape always produces two sibling effects.</remarks>
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

    var loseRaw = match.Groups["lose"].Value;
    var gainRaw = match.Groups["gain"].Value;

    var loseCount = ActivatedRuleHelpers.ParseNumberWord(loseRaw) ?? 1;
    var gainCount = ActivatedRuleHelpers.ParseNumberWord(gainRaw) ?? 1;

    effects = new List<Effect>
    {
      new LoseLifeEffect
      {
        Amount = LiteralQuantity.Of(loseCount),
        Player = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent },
      },
      new GainLifeEffect { Amount = LiteralQuantity.Of(gainCount), Player = ObjectReference.You() },
    };
    return true;
  }
}
