namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;

/// <summary>
/// "You may cast this card from exile." — an unconditional static permission to
/// cast this card from the exile zone (Eternal Scourge, EMN).
///
/// <para>
/// CR 601.3e (verbatim, excerpt): "Some rules and effects state that an alternative
/// set of characteristics or a subset of characteristics are considered to determine
/// if a card or copy of a card is legal to cast…" Here the alternative set of
/// characteristics is the <i>zone</i> the card may be cast from — exile rather than
/// only its hand. The permission does not replace the mana cost, so the produced
/// <see cref="AlternativeCastEffect"/> has <c>FromZone = Exile</c> and a null
/// <see cref="AlternativeCastEffect.Cost"/> (cast for its own mana cost) and a null
/// <see cref="AlternativeCastEffect.Condition"/> (unconditional).
/// </para>
///
/// <para>
/// This is not a keyword ability, so the ability carries no <c>KeywordSource</c>.
/// Priority 983 places it just below <see cref="CastFromGraveyardConditionalRule"/>
/// (985) and <see cref="CastFromMultipleZonesRule"/> (984), which handle overlapping
/// oracle phrasings — multi-zone permission and conditional graveyard permission
/// respectively. A narrower anchor (^…$) ensures this rule does not match either
/// sibling as a substring.
/// </para>
/// </summary>
[StaticRule(Priority = 982)]
public sealed class CastFromExileRule : IStaticRule
{
  // "You may cast this card from exile."
  // Anchored ^…$ to avoid matching "You may cast this card from exile or from …"
  // (handled by CastFromMultipleZonesRule) as a substring.
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+cast\s+this\s+card\s+from\s+exile\.?\s*$",
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
            FromZone = Zone.Exile,
          },
        ],
      },
    ];
  }
}
