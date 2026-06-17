namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;

/// <summary>
/// "You may cast this card from your graveyard or from exile." — an unconditional
/// permission to cast this card from two zones (Squee, the Immortal).
///
/// <para>
/// CR 601.3e (verbatim, excerpt): "Some rules and effects state that an alternative
/// set of characteristics or a subset of characteristics are considered to determine
/// if a card or copy of a card is legal to cast…" Here the permission grants two
/// separate zone exceptions — graveyard and exile — without an alternative cost and
/// without any condition. Each zone becomes its own <see cref="AlternativeCastEffect"/>
/// inside a single <see cref="StaticAbility"/> (the conjunction of the two permissions
/// is expressed by the single oracle sentence).
/// </para>
///
/// <para>
/// The produced <see cref="AlternativeCastEffect"/> nodes each have a null
/// <see cref="AlternativeCastEffect.Cost"/> (cast for the card's own mana cost)
/// and a null <see cref="AlternativeCastEffect.Condition"/> (unconditional).
/// </para>
/// </summary>
[StaticRule(Priority = 984)]
public sealed class CastFromMultipleZonesRule : IStaticRule
{
  // "You may cast this card from your graveyard or from exile."
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+cast\s+this\s+card\s+from\s+your\s+graveyard\s+or\s+from\s+exile\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new AlternativeCastEffect
          {
            FromZone = Zone.Graveyard,
          },
          new AlternativeCastEffect
          {
            FromZone = Zone.Exile,
          },
        ],
      },
    ];
  }
}
