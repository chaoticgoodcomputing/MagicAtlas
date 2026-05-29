namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Put your commander into your hand from the command zone." — Road of Return.
/// </summary>
[SpellRule]
public sealed class ReturnCommanderToHandRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Regex.IsMatch(
        text,
        @"^Put\s+your\s+commander\s+into\s+your\s+hand\s+from\s+the\s+command\s+zone$",
        RegexOptions.IgnoreCase))
    {
      return false;
    }
    effect = new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Designated,
        Filter = new ObjectFilter
        {
          Characteristics = [Characteristic.Other("your commander")],
          Zone = Zone.CommandZone,
        },
      },
    };
    return true;
  }
}
