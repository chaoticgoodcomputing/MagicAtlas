namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;

/// <summary>
/// "You may cast this card from your graveyard as long as [condition]." — the
/// written-out conditional graveyard-recursion permission (Gravecrawler:
/// "You may cast this card from your graveyard as long as you control a Zombie.").
///
/// <para>
/// CR 601.3e (verbatim, excerpt): "Some rules and effects state that an alternative
/// set of characteristics or a subset of characteristics are considered to determine
/// if a card or copy of a card is legal to cast…" Here the alternative set of
/// characteristics is the <i>zone</i> the card may be cast from — its graveyard rather
/// than only its hand. The permission does not replace the mana cost (contrast the
/// alternative-cost keywords Flashback/Escape/Madness), so the produced
/// <see cref="AlternativeCastEffect"/> has <c>FromZone = Graveyard</c> and a null
/// <see cref="AlternativeCastEffect.Cost"/> (cast for its own mana cost).
/// </para>
///
/// <para>
/// The "as long as [condition]" gate is structured through
/// <see cref="ConditionParser"/> (ADR 0007) — "you control a Zombie" becomes a
/// <see cref="CountCondition"/> over Zombies you control (≥1), not a free-text
/// residual — and rides on <see cref="AlternativeCastEffect.Condition"/>, the
/// effect-level precondition home shared with the conditional alternative-cast
/// keywords (Surge/Spectacle/Mayhem). This is not a keyword, so the ability carries
/// no <c>KeywordSource</c>.
/// </para>
/// </summary>
[StaticRule(Priority = 985)]
public sealed class CastFromGraveyardConditionalRule : IStaticRule
{
  // "You may cast this card from your graveyard as long as <cond>."
  // The condition body is everything after "as long as" up to the trailing period.
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+cast\s+this\s+card\s+from\s+your\s+graveyard\s+as\s+long\s+as\s+(?<cond>.+?)\.?\s*$",
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

    var conditionText = match.Groups["cond"].Value.Trim();

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new AlternativeCastEffect
          {
            FromZone = Zone.Graveyard,
            Condition = ConditionParser.Parse(conditionText),
          },
        ],
      },
    ];
  }
}
