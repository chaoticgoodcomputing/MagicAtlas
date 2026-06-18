namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;

/// <summary>
/// "Double target player's life total." — Beacon of Immortality's first clause.
///
/// <para>
/// Rule 701.10d: "To double a player's life total, the player gains or loses an
/// amount of life such that their new life total is twice its current value."
/// MAST records the oracle's phrasing as a <see cref="DoubleLifeTotalEffect"/>;
/// the gain/lose arithmetic is engine territory.
/// </para>
///
/// <para>
/// GUARD: fully anchored (^ … $). Matches only the "Double target player's life
/// total" shape. Does not match "double a creature's power" (different oracle
/// template), "double [your] life total" (no "target"), or any substring of a
/// compound sentence.
/// </para>
/// </summary>
[SpellRule]
public sealed class DoubleLifeTotalSpellRule : ISpellRule
{
  private static readonly Regex TargetPlayerPattern = new(
    @"^Double\s+target\s+player's\s+life\s+total$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc cref="ISpellRule.TryMatch"/>
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!TargetPlayerPattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new DoubleLifeTotalEffect
    {
      Player = ObjectReference.Target(ObjectFilter.Player()),
    };
    return true;
  }
}
