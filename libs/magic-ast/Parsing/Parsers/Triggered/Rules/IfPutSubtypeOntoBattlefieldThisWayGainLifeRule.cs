namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "If you put a [Subtype] onto the battlefield this way, you gain [N] life."
///
/// <para>
/// Handles the Spelunking pattern: a conditional effect sentence (CR 603.12 —
/// reflexive triggered ability) that fires based on whether the card moved to
/// the battlefield by the preceding optional zone-change in the same ability
/// had a particular subtype. Modelled as a <see cref="ConditionalEffect"/> whose
/// <see cref="ConditionalEffect.Condition"/> is an
/// <see cref="OtherCondition"/> (the causation-gate "this way" plus a subtype
/// filter is a compound shape without a dedicated structured arm yet — ADR 0001
/// residual) and whose <see cref="ConditionalEffect.Then"/> is a
/// <see cref="GainLifeEffect"/>.
/// </para>
///
/// <para>
/// CR 603.12 (verbatim): "A resolving spell or ability may allow or instruct a
/// player to take an action and create a triggered ability that triggers 'when [a
/// player] [does or doesn't]' take that action or 'when [something happens] this
/// way.' These reflexive triggered abilities follow the rules for delayed triggered
/// abilities (see rule 603.7), except that they're checked immediately after being
/// created and trigger based on whether the trigger event or events occurred earlier
/// during the resolution of the spell or ability that created them."
/// </para>
///
/// <para>
/// CR 119.3 (verbatim): "If an effect causes a player to gain life or lose life,
/// that player's life total is adjusted accordingly."
/// </para>
///
/// <para>
/// ANCHORED (^…$): fully anchored to prevent false-positive substring matches.
/// Priority 97 — same band as <see cref="DrawThenMayPutLandFromHandRule"/> so
/// both Spelunking sentences are processed before less-specific rules.
/// </para>
/// </summary>
[TriggeredRule(Priority = 97)]
public sealed class IfPutSubtypeOntoBattlefieldThisWayGainLifeRule : ITriggeredRule
{
  // Anchored pattern:
  // "If you put a[n] <subtype> onto the battlefield this way, you gain <N> life."
  // Groups:
  //   subtype: the subtype (e.g. "Cave")
  //   amount:  life amount (digit or word)
  private static readonly Regex _pattern = new(
    @"^If\s+you\s+put\s+a(?:n)?\s+(?<subtype>[A-Z][a-zA-Z]+)\s+onto\s+the\s+battlefield\s+this\s+way,\s+"
    + @"you\s+gain\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, int> _wordNumbers =
    new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
      ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
      ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10,
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var subtype = m.Groups["subtype"].Value.Trim();
    var amountStr = m.Groups["amount"].Value.Trim();

    int amount;
    if (!int.TryParse(amountStr, out amount))
    {
      if (!_wordNumbers.TryGetValue(amountStr, out amount))
      {
        return false;
      }
    }

    // The condition: "you put a [Subtype] onto the battlefield this way" is a
    // within-resolution causation-gate keyed on a subtype of the object the preceding
    // "put a land onto the battlefield" acted on (CR 608.2c). Structured to
    // ActionThisWayCondition (Action=PutOntoBattlefield + subtype filter), the
    // action-general sibling of DiedThisWayCondition.
    var condition = new ActionThisWayCondition
    {
      Action = PrecedingAction.PutOntoBattlefield,
      Filter = new ObjectFilter { Subtypes = [subtype] },
    };

    var gainLife = new GainLifeEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Player = ObjectReference.You(),
    };

    effect = new ConditionalEffect
    {
      Condition = condition,
      Then = gainLife,
    };
    return true;
  }
}
