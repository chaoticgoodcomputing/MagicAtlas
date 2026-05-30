namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Rookie-Mistake shape (single-effect dispatch path):
/// "Until end of turn, target creature gets +0/+2 and another target creature gets -2/-0."
/// Wrapped in a <see cref="CompositeEffect"/>.
/// </summary>
[SpellRule]
public sealed class ModifyPTConjunctionRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^Until\s+end\s+of\s+turn,\s*target\s+creature\s+gets\s+(?<p1>[+-]\d+)/(?<t1>[+-]\d+)\s+and\s+another\s+target\s+creature\s+gets\s+(?<p2>[+-]\d+)/(?<t2>[+-]\d+)$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }
    var p1 = int.Parse(m.Groups["p1"].Value);
    var t1 = int.Parse(m.Groups["t1"].Value);
    var p2 = int.Parse(m.Groups["p2"].Value);
    var t2 = int.Parse(m.Groups["t2"].Value);

    var duration = UntilTimeDuration.EndOfTurn;
    var effects = new List<Effect>
    {
      new ModifyPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["creature"] },
        },
        PowerModifier = LiteralQuantity.Of(p1),
        ToughnessModifier = LiteralQuantity.Of(t1),
        Duration = duration,
      },
      new ModifyPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = [Characteristic.Other("another")],
          },
        },
        PowerModifier = LiteralQuantity.Of(p2),
        ToughnessModifier = LiteralQuantity.Of(t2),
        Duration = duration,
      },
    };

    effect = new CompositeEffect { Effects = effects };
    return true;
  }
}
