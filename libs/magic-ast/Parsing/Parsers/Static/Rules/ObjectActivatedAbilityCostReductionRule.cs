namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Static cost-reduction ability that lowers the activation cost for all
/// activated abilities belonging to a category of permanents the controller
/// controls, with an optional mana floor.
///
/// <para>
/// Canonical pattern (Forensic Gadgeteer, CR 602.1):
/// "Activated abilities of <see cref="ObjectFilter"/> you control cost {N}
/// less to activate[. This effect can't reduce the mana in that cost to less
/// than one mana]."
/// </para>
///
/// <para>
/// CR 602.1c: "An activated ability is the only kind of ability that can be
/// activated." The reference here is to all activated abilities on permanents
/// matching the type filter, not to a specific keyword's activated ability
/// (contrast <see cref="AppliesToCostReductionRule"/> which keys on a keyword
/// identity). Encoded as
/// <see cref="ObjectActivatedAbilityReference.PermanentFilter"/> on a
/// <see cref="CostReductionEffect"/>.
/// </para>
///
/// <para>
/// The optional trailing sentence "This effect can't reduce the mana in that
/// cost to less than one mana." becomes
/// <see cref="CostReductionEffect.MinimumManaCost"/> = 1.
/// </para>
/// </summary>
[StaticRule(Priority = 991)]
public sealed class ObjectActivatedAbilityCostReductionRule : IStaticRule
{
  // "Activated abilities of <CardType> you control cost {N} less to activate."
  // The card-type noun is a single capitalised word (Artifact, Creature, …).
  // Anchored start-to-end so it cannot fire as a substring of a longer clause.
  private static readonly Regex _pattern = new(
    @"^\s*Activated\s+abilities\s+of\s+(?<type>[A-Za-z]+s?)\s+you\s+control\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+activate\s*\.\s*(?<floor>This\s+effect\s+can't\s+reduce\s+the\s+mana\s+in\s+that\s+cost\s+to\s+less\s+than\s+one\s+mana\s*\.)?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var text = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(text);
    if (!match.Success)
    {
      return null;
    }

    var typeNoun = match.Groups["type"].Value.Trim();
    var amount = int.Parse(match.Groups["amount"].Value);
    var hasFloor = match.Groups["floor"].Success && match.Groups["floor"].Length > 0;

    // Singularize: "artifacts" → "artifact"
    var singular = Singularize(typeNoun);

    // We only handle card-type nouns for now (artifact, creature, enchantment, etc.).
    // For an unrecognised noun fall through to the fallback so we don't silently
    // mis-parse a future shape.
    var filter = BuildPermanentFilter(singular);
    if (filter is null)
    {
      return null;
    }

    var effect = new CostReductionEffect
    {
      Amount = LiteralQuantity.Of(amount),
      AppliesTo = new ObjectActivatedAbilityReference
      {
        PermanentFilter = filter,
      },
      MinimumManaCost = hasFloor ? 1 : null,
    };

    return
    [
      new StaticAbility
      {
        Effects = [effect],
      },
    ];
  }

  // Card types the rule recognises as permanent-filter nouns (lowercased on
  // the ObjectFilter.CardTypes axis, matching the corpus convention).
  private static readonly HashSet<string> _permanentCardTypes =
    new(StringComparer.OrdinalIgnoreCase)
    {
      "artifact", "creature", "enchantment", "planeswalker", "land",
      "battle", "permanent",
    };

  /// <summary>
  /// Builds the permanent filter for a given card-type noun.
  /// Returns null when the noun is not a recognised card type.
  /// </summary>
  private static ObjectFilter? BuildPermanentFilter(string cardType)
  {
    if (!_permanentCardTypes.Contains(cardType))
    {
      return null;
    }
    return new ObjectFilter
    {
      CardTypes = [cardType.ToLowerInvariant()],
      Controller = ControllerFilter.You,
    };
  }

  // Conservative singularization: strip a trailing "s" only when the result
  // is a recognised card type. Avoids stripping on words like "class".
  private static string Singularize(string noun)
  {
    if (noun.EndsWith('s') && noun.Length > 1)
    {
      var candidate = noun[..^1];
      if (_permanentCardTypes.Contains(candidate))
      {
        return candidate;
      }
    }
    return noun;
  }
}
