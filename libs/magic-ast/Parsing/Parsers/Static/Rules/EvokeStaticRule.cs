namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing;

/// <summary>
/// Decomposes an "Evoke [cost]" oracle line into the two abilities defined by
/// CR 702.74a — a static alternative-cast ability and an ETB-sacrifice triggered
/// ability.
///
/// <para>
/// CR 702.74a (verbatim): "Evoke represents two abilities: a static ability that
/// functions in any zone from which the card with evoke can be cast and a triggered
/// ability that functions on the battlefield. 'Evoke [cost]' means 'You may cast
/// this card by paying [cost] rather than paying its mana cost' and 'When this
/// permanent enters, if its evoke cost was paid, its controller sacrifices it.'
/// Casting a spell for its evoke cost follows the rules for paying alternative
/// costs in rules 601.2b and 601.2f-h."
/// </para>
///
/// <para>
/// Priority 1001 — fires before <see cref="KeywordListRule"/> (priority 1000) so
/// the two-ability decomposition takes precedence over the single-ability keyword
/// combinator path. "if its evoke cost was paid" is the generalized cost-paid
/// reference node <see cref="KeywordCostPaidCondition"/> keyed on Evoke
/// (reference-not-resolution, ADR 0004); both abilities carry
/// <see cref="KeywordAbility.Evoke"/> as their <c>KeywordSource</c> (keyword-inherent,
/// mirroring the Reconfigure precedent where both abilities carry their KeywordSource).
/// </para>
/// </summary>
[StaticRule(Priority = 1001)]
public sealed class EvokeStaticRule : IStaticRule
{
  // Matches: "Evoke {cost}" (mana-cost evoke) with optional trailing reminder text.
  private static readonly Regex _manaCostPattern = new(
    @"^\s*Evoke\s+(?<cost>(?:\{[^}]+\})+)\s*(?<reminder>\(.*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches: "Evoke—Exile a [color] card from your hand." — the non-mana evoke
  // cost template used by the Modern Horizons 2 Elemental Incarnations (Fury,
  // Solitude, Subtlety, Endurance). The em dash (—) separates the keyword
  // from a prose cost, mirroring the general "Keyword—[cost]" template used
  // whenever a CR-702 keyword's cost isn't purely mana symbols. Anchored
  // (^...$) to this exact surface so it cannot collide with any other cost
  // shape; a future non-color exile/discard evoke cost (e.g. Grief's "Evoke—
  // Discard a card.") is a distinct pattern to add alongside this one, not a
  // reason to broaden it.
  private static readonly Regex _exileColorCardCostPattern = new(
    @"^\s*Evoke\s*—\s*Exile\s+a\s+(?<color>white|blue|black|red|green)\s+card\s+from\s+your\s+hand\.?\s*(?<reminder>\(.*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    Cost cost;
    Parenthetical? reminder = null;

    var manaMatch = _manaCostPattern.Match(clause.RawText);
    if (manaMatch.Success)
    {
      var costStr = manaMatch.Groups["cost"].Value;
      try
      {
        var parsed = new ManaCostParser().Parse(costStr);
        if (parsed.Symbols.Count == 0)
        {
          return null;
        }
        cost = new ManaCost { Symbols = parsed.Symbols };
      }
      catch
      {
        return null;
      }

      var manaReminderGroup = manaMatch.Groups["reminder"];
      if (manaReminderGroup.Success && manaReminderGroup.Value.Length > 0)
      {
        reminder = new Parenthetical { Text = manaReminderGroup.Value };
      }
    }
    else
    {
      // Non-mana evoke cost (CR 702.74a permits any cost, not just mana):
      // "Exile a [color] card from your hand" — the color-pitch shape shared
      // with the free-spell alternative-cost family (Force of Will et al.),
      // modelled with the same Filter/Quantity/FromZone ExileCost shape.
      var exileMatch = _exileColorCardCostPattern.Match(clause.RawText);
      if (!exileMatch.Success)
      {
        return null;
      }

      var colorCode = exileMatch.Groups["color"].Value.ToLowerInvariant() switch
      {
        "white" => "W",
        "blue" => "U",
        "black" => "B",
        "red" => "R",
        "green" => "G",
        _ => throw new InvalidOperationException("Unreachable: regex only matches the five color words."),
      };

      cost = new ExileCost
      {
        Filter = new ObjectFilter { CardTypes = ["card"], Colors = [colorCode] },
        Quantity = LiteralQuantity.Of(1),
        FromZone = Zone.Hand,
      };

      var exileReminderGroup = exileMatch.Groups["reminder"];
      if (exileReminderGroup.Success && exileReminderGroup.Value.Length > 0)
      {
        reminder = new Parenthetical { Text = exileReminderGroup.Value };
      }
    }

    // Ability 1 (CR 702.74a, first clause): the static alternative-cast ability.
    // "You may cast this card by paying [cost] rather than paying its mana cost."
    var altCastAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Evoke,
      Effects =
      [
        new AlternativeCastEffect
        {
          FromZone = Zone.Hand,
          Cost = cost,
        },
      ],
      Reminder = reminder,
    };

    // Ability 2 (CR 702.74a, second clause): the ETB-sacrifice triggered ability.
    // "When this permanent enters, if its evoke cost was paid, its controller
    //  sacrifices it." The intervening-if is the generalized cost-paid reference
    //  node keyed on Evoke (reference-not-resolution, ADR 0004).
    var sacrificeAbility = new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Evoke,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.When,
        Event = TriggerEvent.Enters,
      },
      InterveningIf = new KeywordCostPaidCondition { Keyword = KeywordAbility.Evoke },
      Effects =
      [
        new SacrificeEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
        },
      ],
    };

    return [altCastAbility, sacrificeAbility];
  }
}
