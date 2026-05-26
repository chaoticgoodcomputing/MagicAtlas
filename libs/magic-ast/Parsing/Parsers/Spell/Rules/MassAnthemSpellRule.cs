namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the mass P/T-modification shape (all creatures you control):
///   "Creatures you control get +N/+M until end of turn."
///   "Creatures you control get -N/-M until end of turn."
///
/// This is the mass anthem version. The single-target "Target creature gets +N/+M ..."
/// shape is handled separately by <see cref="ModifyPTSpellRule"/>.
///
/// Examples:
/// <list type="bullet">
///   <item>"Creatures you control get +1/+1 until end of turn."  (Charge)</item>
///   <item>"Creatures you control get +0/+4 until end of turn."  (Bar the Door)</item>
///   <item>"Creatures you control get +2/+2 until end of turn."  (Righteous Charge)</item>
///   <item>"Creatures you control get +2/+0 until end of turn."  (Desperate Charge)</item>
///   <item>"Creatures you control get +0/+5 until end of turn."  (Solidarity)</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class MassAnthemSpellRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Creatures\s+you\s+control\s+get\s+(?<p>[+\-]\d+)/(?<t>[+\-]\d+)\s+until\s+end\s+of\s+turn$",
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

    var power = int.Parse(m.Groups["p"].Value);
    var toughness = int.Parse(m.Groups["t"].Value);

    effect = new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
        },
      },
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = new UntilEndOfTurnDuration(),
    };
    return true;
  }
}
