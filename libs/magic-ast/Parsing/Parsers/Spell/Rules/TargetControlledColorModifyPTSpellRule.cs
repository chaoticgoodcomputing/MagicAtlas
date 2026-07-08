namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the colour-and-controller qualified P/T-modification shape:
///   "Target green creature you control gets +N/+M until end of turn."
///
/// This is distinct from <see cref="ModifyPTSpellRule"/>'s bare "Target creature
/// gets +N/+M" form — the colour word (CR 105.2) and the "you control" controller
/// restriction (CR 109.5 / 601.2c) both narrow the target's <see cref="ObjectFilter"/>.
/// The mono-colour restriction is carried on <see cref="ObjectFilter.Colors"/> (an
/// object passes if it has at least one of the listed colours, CR 105.1) and the
/// controller restriction on <see cref="ObjectFilter.Controller"/>.
///
/// Examples:
/// <list type="bullet">
///   <item>"Target green creature you control gets +2/+2 until end of turn."  (Hunt the Hunter — first sentence)</item>
/// </list>
///
/// Anchored with <c>^…$</c>; the mandatory "&lt;colour&gt; creature you control"
/// segment keeps it from matching the bare "Target creature gets …" surface handled
/// by <see cref="ModifyPTSpellRule"/>.
/// </summary>
[SpellRule]
public sealed class TargetControlledColorModifyPTSpellRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Target\s+(?<color>white|blue|black|red|green)\s+creature\s+you\s+control\s+gets\s+(?<p>[+\-]\d+)/(?<t>[+\-]\d+)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, string> ColorToCode =
    new(StringComparer.OrdinalIgnoreCase)
    {
      { "white", "W" },
      { "blue", "U" },
      { "black", "B" },
      { "red", "R" },
      { "green", "G" },
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    var color = ColorToCode[m.Groups["color"].Value];
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
          Colors = [color],
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
