namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Format;

/// <summary>
/// "A deck can have any number of cards named [this]." — the self-referential
/// exception to the four-of deck-construction rule (Relentless Rats, Rat Colony,
/// Shadowborn Apostle, Persistent Petitioners, Dragon's Approach).
///
/// <para>
/// CR 100.2a: "In constructed play …, each deck has a minimum deck size of 60
/// cards. A constructed deck may contain any number of basic land cards and no
/// more than four of any card with a particular English name other than basic
/// land cards." This static line lifts that limit for cards sharing this card's
/// name. It is a characteristic-defining-style declarative property of the card,
/// so it lands as a <see cref="StaticAbility"/> — not an activated or triggered
/// ability.
/// </para>
///
/// <para>
/// The "[this]" in "named [this]" always names the card the line appears on, so
/// the emitted <see cref="AnyNumberInDeckEffect"/> carries no field: the self-
/// reference is implicit. The regex tolerates any card name in that slot rather
/// than threading the card's own name through (it is structurally always self).
/// </para>
/// </summary>
[StaticRule(Priority = 50)]
public sealed class AnyNumberInDeckRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*A\s+deck\s+can\s+have\s+any\s+number\s+of\s+cards\s+named\s+.+?\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new AnyNumberInDeckEffect()],
      },
    ];
  }
}
