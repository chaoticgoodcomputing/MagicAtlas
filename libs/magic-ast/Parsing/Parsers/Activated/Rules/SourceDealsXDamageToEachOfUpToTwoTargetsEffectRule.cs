namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[Source] deals X damage to each of up to two targets." — the payoff of a
/// variable-loyalty planeswalker ability (Chandra, Hope's Beacon's "−X: Chandra
/// deals X damage to each of up to two targets."). Each of the (up to two) chosen
/// targets independently receives X damage — the amount is NOT divided (CR 601.2c:
/// "each of up to two targets" is a variable count of independent targets, distinct
/// from CR 601.2d "divided as you choose").
///
/// <para>
/// The X of the damage is the same X paid as the ability's loyalty cost (CR 606.5 /
/// CR 107.3 — an X in an ability's cost fixes X for its effect). MAST records the
/// amount as <see cref="VariableQuantity"/> X (reference-not-resolution, ADR 0004);
/// the engine binds X at activation.
/// </para>
///
/// <para>
/// "up to two targets" is a bare "target" — i.e. "any target" (creature, player,
/// planeswalker, or battle) — so the recipient is
/// <see cref="ObjectReferenceKind.AnyTarget"/> carrying an
/// <see cref="UpToQuantity"/> of two. The source ("Chandra") is the ability's own
/// permanent, modeled as <see cref="ObjectReferenceKind.Self"/>; the leading name is
/// captured only to anchor the sentence, not emitted.
/// </para>
///
/// <para>ANCHORED (<c>^…$</c>): the sentence must end exactly in "deals X damage to
/// each of up to two targets", so the rule cannot claim a substring of a more
/// specific damage sentence.</para>
/// </summary>
[ActivatedEffectRule(Priority = 955)]
public sealed class SourceDealsXDamageToEachOfUpToTwoTargetsEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^(?<source>.+?)\s+deals\s+X\s+damage\s+to\s+each\s+of\s+up\s+to\s+two\s+targets$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var m = Pattern.Match(effectText.Trim().TrimEnd('.').Trim());
    if (!m.Success)
    {
      return null;
    }

    return new DealDamageEffect
    {
      Source = ObjectReference.Self(),
      Amount = VariableQuantity.X,
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.AnyTarget,
        Quantity = new UpToQuantity { Maximum = 2, Minimum = 0 },
      },
    };
  }
}
