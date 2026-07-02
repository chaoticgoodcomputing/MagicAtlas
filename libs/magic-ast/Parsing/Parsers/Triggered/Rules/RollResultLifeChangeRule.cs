namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// The life-changing CONSUMER of a die roll — the second sentence in the ETB-roll family
/// ("When this creature enters, roll a [die]. [life change]"). The roll itself is the
/// <see cref="MagicAST.AST.Effects.Dice.RollDieEffect"/> emitted by
/// <see cref="RollDieTriggeredRule"/>; this rule recognises the follow-up sentence that
/// spends the roll's result on a life total. The multi-sentence dispatcher in
/// <see cref="MagicAST.Parsing.Parsers.TriggeredAbilityParser"/> composes the two into one
/// effect list — exactly as Ancient Copper Dragon's <c>[rollDie, createToken{result}]</c> is
/// composed.
///
/// <para>
/// Two surfaces are handled, generalised over life direction (gain/lose):
/// <list type="bullet">
///   <item><b>Bare consumer</b> — "you gain/lose life equal to the result"
///   (Adorable Kitten: "You gain life equal to the result."). The amount is the roll result,
///   modelled as <see cref="DieRollResultQuantity"/> (CR 706.2 — "the result of the die roll";
///   CR 706.4 — abilities without a results table indicate how to use the result).</item>
///   <item><b>Result-gated consumer</b> — "if the result is N or less/more, you
///   gain/lose that much life" (Dissatisfied Customer: "If the result is 3 or less,
///   you lose that much life."). The gate is a <see cref="QuantityComparisonCondition"/>
///   comparing the roll result (<see cref="DieRollResultQuantity"/>) against the literal
///   threshold; "that much" is the roll result again (the antecedent of "that much" is the
///   roll, CR 706.2). Mirrors Captain Rex Nebula's <c>conditional{quantityComparison{result},
///   …{result}}</c> shape.</item>
/// </list>
/// </para>
///
/// <para>
/// Scoped to the controller ("you"): both pinning golds spend the result on the ability's
/// controller. The targeted-player forms ("target opponent loses life equal to the result",
/// Big Boa Constrictor) are a sibling slice that needs its own gold to fix the targeting
/// shape — deliberately NOT generalised here so every branch is gold-pinned.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the textual reference to the roll
/// result; the engine reads the actual rolled value at resolution. CR 119.3 — gaining or
/// losing life adjusts the player's life total accordingly.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>). Priority 74: in the dice-result-consumer band beside
/// <see cref="CreateTokensEqualToDieResultTriggeredRule"/> (72), above the generic life
/// rules so the "equal to the result" / "that much" forms are claimed by this die-aware
/// rule rather than mis-bound to a fixed-amount sibling.
/// </para>
/// </summary>
[TriggeredRule(Priority = 74)]
public sealed class RollResultLifeChangeRule : ITriggeredRule
{
  // "you gain/lose life equal to the result"
  private static readonly Regex _bare = new(
    @"^you\s+(?<verb>gain|lose)\s+life\s+equal\s+to\s+the\s+result$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "if the result is N or less/more (or higher/lower), you gain/lose that much life"
  private static readonly Regex _gated = new(
    @"^if\s+the\s+result\s+is\s+(?<n>\d+)\s+or\s+(?<dir>less|fewer|lower|more|greater|higher),\s+"
      + @"you\s+(?<verb>gain|lose)\s+that\s+much\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    var bare = _bare.Match(trimmed);
    if (bare.Success)
    {
      effect = BuildLifeEffect(bare.Groups["verb"].Value, new DieRollResultQuantity());
      return true;
    }

    var gated = _gated.Match(trimmed);
    if (gated.Success && int.TryParse(gated.Groups["n"].Value, out var threshold))
    {
      effect = new ConditionalEffect
      {
        Condition = new QuantityComparisonCondition
        {
          Left = new DieRollResultQuantity(),
          Operator = ThresholdOperator(gated.Groups["dir"].Value),
          Right = LiteralQuantity.Of(threshold),
        },
        Then = BuildLifeEffect(gated.Groups["verb"].Value, new DieRollResultQuantity()),
      };
      return true;
    }

    return false;
  }

  // "N or less/fewer/lower" ⇒ result ≤ N; "N or more/greater/higher" ⇒ result ≥ N.
  private static ComparisonOperator ThresholdOperator(string dir) =>
    dir.ToLowerInvariant() switch
    {
      "less" or "fewer" or "lower" => ComparisonOperator.LessThanOrEqual,
      _ => ComparisonOperator.GreaterThanOrEqual,
    };

  private static Effect BuildLifeEffect(string verb, Quantity amount)
  {
    var isGain = verb.Equals("gain", StringComparison.OrdinalIgnoreCase);
    return isGain
      ? new GainLifeEffect { Amount = amount, Player = ObjectReference.You() }
      : new LoseLifeEffect { Amount = amount, Player = ObjectReference.You() };
  }
}
