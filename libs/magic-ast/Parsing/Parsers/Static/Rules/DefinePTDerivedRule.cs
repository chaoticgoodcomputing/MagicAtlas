namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;

/// <summary>
/// A characteristic-defining ability (CDA) that sets a creature's power and/or
/// toughness to a <see cref="DerivedQuantity"/> — a hand or graveyard size, not a
/// board-object count. Handles the oracle pattern:
/// <list type="bullet">
/// <item>"[Name]'s power and toughness are each equal to the number of cards in your hand."
///   → <see cref="PTCharacteristic.Both"/>, <see cref="DerivedKind.CardsInHand"/></item>
/// <item>"[Name]'s power is equal to the number of cards in your hand."
///   → <see cref="PTCharacteristic.Power"/>, <see cref="DerivedKind.CardsInHand"/></item>
/// <item>"[Name]'s toughness is equal to the number of cards in your hand."
///   → <see cref="PTCharacteristic.Toughness"/>, <see cref="DerivedKind.CardsInHand"/></item>
/// </list>
///
/// <para>
/// Rule 604.3: "A characteristic-defining ability defines a characteristic value for
/// the object it's on." Rule 107.3: the value of * in a CDA box is determined by an
/// ability (layer 7a, Rule 613.1a). The value here is derived from the controller's
/// hand size — a game-state quantity evaluated continuously.
/// </para>
///
/// <para>
/// Priority 976 is higher than <see cref="DefinePTRule"/> (Priority 975) so this
/// derived-value branch fires first; if neither derived pattern matches, control
/// falls through to <see cref="DefinePTRule"/>'s board-count branch.
/// </para>
/// </summary>
[StaticRule(Priority = 976)]
public sealed class DefinePTDerivedRule : IStaticRule
{
  // "[Name]'s power and toughness are each equal to the number of cards in your hand."
  private static readonly Regex _bothPattern = new(
    @"^\s*.+?'s\s+power\s+and\s+toughness\s+are\s+each\s+equal\s+to\s+the\s+number\s+of\s+(?<phrase>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "[Name]'s (power|toughness) is equal to the number of cards in your hand."
  private static readonly Regex _singlePattern = new(
    @"^\s*.+?'s\s+(?<which>power|toughness)\s+is\s+equal\s+to\s+the\s+number\s+of\s+(?<phrase>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "cards in your hand" — the hand-size CDA (e.g. Psychosis Crawler, Maro).
  // Source is omitted (implicit self-controller "your" is the default).
  private static readonly Regex _cardsInHandPhrase = new(
    @"^cards?\s+in\s+your\s+hand$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Try "power and toughness are each equal to" first (most specific).
    var bothMatch = _bothPattern.Match(clause.RawText);
    if (bothMatch.Success)
    {
      var quantity = ClassifyDerivedPhrase(bothMatch.Groups["phrase"].Value.Trim());
      if (quantity is null)
      {
        return null;
      }
      return
      [
        new StaticAbility
        {
          Effects = [new DefinePTEffect
          {
            Characteristic = PTCharacteristic.Both,
            Value = quantity,
          }],
        },
      ];
    }

    // Try "power is equal to" or "toughness is equal to".
    var singleMatch = _singlePattern.Match(clause.RawText);
    if (singleMatch.Success)
    {
      var quantity = ClassifyDerivedPhrase(singleMatch.Groups["phrase"].Value.Trim());
      if (quantity is null)
      {
        return null;
      }
      var which = singleMatch.Groups["which"].Value.ToLowerInvariant();
      var characteristic = which == "power"
        ? PTCharacteristic.Power
        : PTCharacteristic.Toughness;
      return
      [
        new StaticAbility
        {
          Effects = [new DefinePTEffect
          {
            Characteristic = characteristic,
            Value = quantity,
          }],
        },
      ];
    }

    return null;
  }

  /// <summary>
  /// Maps a post-"the number of" phrase to a <see cref="DerivedQuantity"/> for
  /// recognized derived-value CDAs. Returns <see langword="null"/> for board-object
  /// count phrases (those fall through to <see cref="DefinePTRule"/>).
  /// </summary>
  private static DerivedQuantity? ClassifyDerivedPhrase(string phrase)
  {
    if (_cardsInHandPhrase.IsMatch(phrase))
    {
      return new DerivedQuantity { DerivedFrom = DerivedKind.CardsInHand };
    }

    return null;
  }
}
