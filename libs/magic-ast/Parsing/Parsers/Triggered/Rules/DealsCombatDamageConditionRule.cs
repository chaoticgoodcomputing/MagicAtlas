namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever [subject] deals combat damage to (a player|an opponent|any player)" —
/// emits <see cref="TriggerEvent.DealsCombatDamageToPlayer"/> (Rule 510 — Combat
/// Damage Step; Rule 603.6 — triggered abilities). The recipient class is implied
/// by the enum value; the Filter captures the subject (what is dealing the damage).
///
/// <para>
/// "a creature you control with [keyword] deals combat damage to a player" —
/// the "with [keyword]" suffix on the subject noun phrase is a keyword-ability
/// characteristic constraint on the matched object (CR 702 — keyword abilities).
/// Modelled as <see cref="ObjectFilter.Characteristics"/> carrying a structured
/// <see cref="KeywordCharacteristic"/> so the filter is fully typed with no
/// free-text residual.
/// </para>
///
/// <para>
/// CR 510.1 (Combat Damage Step): "First, the active player announces how each
/// attacking creature assigns its combat damage … This turn-based action doesn't
/// use the stack." CR 603.2: "Whenever a game event or game state matches a
/// triggered ability's trigger event, that ability automatically triggers."
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 985)]
public sealed class DealsCombatDamageConditionRule : ITriggerConditionRule
{
  // Matches "with <keyword>" inside a trigger subject phrase, e.g.
  // "a creature you control with deathtouch deals combat damage".
  // Anchored to stop at "deals" so the recipient side is not captured.
  private static readonly Regex _withKeywordPattern = new(
    @"\bwith\s+(?<kw>[a-z]+)\b(?=.*\bdeals\b)",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches the "[CardName] deals combat damage" shape where the card refers to
  // itself by its printed name (CR 201.4). The name is one or more capitalised
  // words (optionally separated by function words such as "of", "the", "a",
  // potentially ending with a comma for legendary epithets), followed by "deals".
  // This is the analogue of IsSelfByNameTrigger for the "deals combat damage"
  // verb, which the general ParseObjectFilter helper does not include.
  private static readonly Regex _namedSourceDealsCombatDamagePattern = new(
    @"^(?:When(?:ever)?\s+)?[A-Z][A-Za-z'\-]*,?(?:\s+(?:[A-Z][A-Za-z'\-]*|of|the|a|an|from|for|to|in|at|with|by|and|or|as),?)*\s+deals\s+combat\s+damage\b",
    RegexOptions.Compiled | RegexOptions.CultureInvariant
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("deals combat damage"))
    {
      return null;
    }

    // Require a player-class recipient: "to a player", "to an opponent", "to any player".
    if (
      !lower.Contains("to a player")
      && !lower.Contains("to an opponent")
      && !lower.Contains("to any player")
    )
    {
      return null;
    }

    // Subject is the thing doing the dealing: "this creature", self-by-name,
    // "a creature you control", etc.
    var filter = TriggeredRuleHelpers.ParseObjectFilter(triggerText);

    // If ParseObjectFilter returned null, try the named-source self-reference
    // shape: "Hapatra deals combat damage to a player". CR 201.4 — a card
    // naming itself in its own oracle text is a self-reference (IsSelf).
    // This verb ("deals") is not in IsSelfByNameTrigger's list (which covers
    // "enters|dies|attacks|blocks"), so we detect it here on the condition-rule
    // side rather than modifying the shared helper.
    if (filter is null && _namedSourceDealsCombatDamagePattern.IsMatch(triggerText))
    {
      filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        IsSelf = true,
      };
    }
    if (filter == null)
    {
      return null;
    }

    // "a creature you control with [keyword] deals combat damage to a player" —
    // augment the filter with the keyword characteristic. Only promote to a
    // structured KeywordCharacteristic when the label is a known keyword ability
    // (Characteristic.FromLabel returns KeywordCharacteristic for recognised
    // keywords, OtherCharacteristic for unknowns). The unknown-label case is
    // dropped here: the caller gets no Characteristics rather than a free-text
    // residual, satisfying the no-free-text invariant for this structured axis.
    var withMatch = _withKeywordPattern.Match(lower);
    if (withMatch.Success)
    {
      var kwLabel = withMatch.Groups["kw"].Value;
      var characteristic = Characteristic.FromLabel(kwLabel);
      if (characteristic is KeywordCharacteristic kc)
      {
        filter = filter with { Characteristics = [kc] };
      }
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.DealsCombatDamageToPlayer,
      Filter = filter,
    };
  }
}
