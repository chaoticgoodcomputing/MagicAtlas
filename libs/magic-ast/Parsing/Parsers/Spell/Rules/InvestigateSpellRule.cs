namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;

/// <summary>
/// "Investigate." — Rule 701.28 keyword action. Creates a Clue token.
/// Oracle lines containing only the keyword action (possibly with a reminder-text
/// parenthetical, which is stripped by SpellAbilityParser before dispatch)
/// produce a bare <see cref="InvestigateEffect"/>.
/// </summary>
[SpellRule(Priority = 60)]
public sealed class InvestigateSpellRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!text.Equals("Investigate", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }
    effect = new InvestigateEffect { IsOptional = false };
    return true;
  }
}
