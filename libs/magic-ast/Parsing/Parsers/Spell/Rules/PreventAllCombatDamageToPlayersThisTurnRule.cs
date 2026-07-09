namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.References;

/// <summary>
/// "Prevent all combat damage that would be dealt to players this turn." (Commencement of
/// Festivities, Defend the Hearth). A Fog-effect instant (CR 615.1) narrowed to only the damage
/// that would be dealt to players — combat damage aimed at creatures/planeswalkers still applies.
/// Distinct from the unrestricted <see cref="PreventAllCombatDamageThisTurnRule"/> ("this turn",
/// no recipient qualifier) and from single-recipient shields ("to target creature"/"to you"):
/// the "players" plural with no "target" keyword is an untargeted blanket over every player,
/// modeled as <see cref="ObjectReferenceKind.EachPlayer"/>.
/// </summary>
[SpellRule]
public sealed class PreventAllCombatDamageToPlayersThisTurnRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Prevent\s+all\s+combat\s+damage\s+that\s+would\s+be\s+dealt\s+to\s+players\s+this\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text))
    {
      return false;
    }

    effect = new PreventDamageEffect
    {
      All = true,
      CombatOnly = true,
      Target = new ObjectReference { Kind = ObjectReferenceKind.EachPlayer },
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
