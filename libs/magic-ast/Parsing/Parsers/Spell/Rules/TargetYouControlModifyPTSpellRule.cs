namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the controller-qualified (but uncolored) P/T-modification shape:
///   "Target creature you control gets +N/+M until end of turn."
///
/// This is the "you control" analogue of <see cref="ModifyPTSpellRule"/>'s bare
/// "Target creature gets +N/+M" form: the "you control" restriction (CR 109.5)
/// narrows the target's <see cref="ObjectFilter.Controller"/> to
/// <see cref="ControllerFilter.You"/>. It emits a <see cref="ModifyPTEffect"/>, a
/// continuous effect (CR 611.1) that adjusts the creature's power/toughness
/// characteristic (CR 613) for the stated duration.
///
/// Distinct from <see cref="TargetControlledColorModifyPTSpellRule"/> (which also
/// requires a colour word before "creature") and from
/// <see cref="ModifyPTAndGainKeywordControlledSpellRule"/> (the "…and gains
/// [keyword]" composite). Anchored <c>^…$</c> so the bare "Target creature gets …"
/// surface and the "…and gains …" composite both stay with their own rules.
///
/// Example:
/// <list type="bullet">
///   <item>"Target creature you control gets +1/+0 until end of turn."  (Ambuscade — first sentence)</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class TargetYouControlModifyPTSpellRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Target\s+creature\s+you\s+control\s+gets\s+(?<p>[+\-]\d+)/(?<t>[+\-]\d+)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    var power = int.Parse(m.Groups["p"].Value);
    var toughness = int.Parse(m.Groups["t"].Value);

    effect = new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
        },
      },
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
