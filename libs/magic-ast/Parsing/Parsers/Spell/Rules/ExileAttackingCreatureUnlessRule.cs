namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile target attacking creature unless its controller pays {X}." — Excise.
/// </summary>
[SpellRule]
public sealed class ExileAttackingCreatureUnlessRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^Exile\s+target\s+attacking\s+creature\s+unless\s+its\s+controller\s+pays\s+\{(?<unlessx>[A-Za-z])\}$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }
    effect = MagicAST.AST.Effects.Core.EffectWrap.Preventable(new ExileEffect {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = [Characteristic.Other("attacking")],
        },
      }}, new UnlessClause
      {
        Player = new ObjectReference { Kind = ObjectReferenceKind.Controller },
        Cost = new ManaCost { Symbols = [new ManaSymbol { Kind = ManaSymbolKind.Variable }] },
      });
    return true;
  }
}
