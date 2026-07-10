namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Parsers.Activated;

/// <summary>
/// Decomposes a "Buyback [cost]" oracle line into the two static abilities defined by
/// CR 702.27a (mirroring the <see cref="DashStaticRule"/> / <see cref="ReconfigureStaticRule"/>
/// multi-ability precedent).
///
/// <para>
/// CR 702.27a (verbatim): "Buyback appears on some instants and sorceries. It represents
/// two static abilities that function while the spell is on the stack. 'Buyback [cost]'
/// means 'You may pay an additional [cost] as you cast this spell' and 'If the buyback
/// cost was paid, put this spell into its owner's hand instead of into that player's
/// graveyard as it resolves.'"
/// </para>
///
/// <para>
/// Priority 1001 — fires before <see cref="KeywordListRule"/> (priority 1000) so the
/// two-ability decomposition takes precedence over the single-ability keyword combinator
/// path. The combinator in <see cref="MagicAST.Keywords.Definitions.BuybackKeyword"/>
/// remains live as a fallback, emitting only the primary additional-cast-cost static
/// ability (the keyword expander returns a single <c>Ability</c>; Buyback decomposes into
/// two).
/// </para>
///
/// <para>
/// The buyback cost is usually mana ("Buyback {3}") but CR 702.27a's "[cost]" also covers
/// non-mana costs, printed with an em dash rather than trailing the keyword directly —
/// "Buyback—Sacrifice a land." (Pegasus Stampede). Both forms share the same two-ability
/// decomposition; only the <see cref="Cost"/> payload of ability 1 differs. The non-mana
/// cost text is delegated to the shared
/// <see cref="ActivatedRuleHelpers.ParseSacrificePattern"/> helper (same primitive an
/// activation cost "Sacrifice a land" would use) rather than re-deriving sacrifice-cost
/// parsing here.
/// </para>
///
/// <para>
/// Ability 2 is a zone-change replacement effect (CR 614 / 702.27a): the spell would go
/// from the stack to the graveyard as it resolves, but instead goes to its owner's hand.
/// Modeled with the existing replacement primitives — <see cref="ReplacementEffect"/> over
/// a <see cref="ZoneChangeEvent"/> (origin stack, destination graveyard) with
/// <c>OriginalEventOccurs = false</c> ("instead") and the <see cref="ReturnToHandEffect"/>
/// as the replacement action — gated on <see cref="KeywordCostPaidCondition"/> for Buyback.
/// </para>
/// </summary>
[StaticRule(Priority = 1001)]
public sealed class BuybackStaticRule : IStaticRule
{
  // Matches: "Buyback {cost}" with optional trailing reminder text.
  // The cost group captures one or more mana symbols, e.g. "{3}".
  private static readonly Regex _manaPattern = new(
    @"^\s*Buyback\s+(?<cost>(?:\{[^}]+\})+)\s*(?<reminder>\(.*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches: "Buyback—[non-mana cost text]." with optional trailing reminder text, e.g.
  // "Buyback—Sacrifice a land. (…)" (Pegasus Stampede). The em dash (—) separates the
  // keyword from a prose cost rather than mana symbols directly abutting it; the cost
  // clause ends at the period, before any parenthetical reminder.
  private static readonly Regex _nonManaPattern = new(
    @"^\s*Buyback\s*[—-]\s*(?<cost>[^.(]+)\.\s*(?<reminder>\(.*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    Cost cost;
    Parenthetical? reminder = null;

    var manaMatch = _manaPattern.Match(clause.RawText);
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

      var reminderGroup = manaMatch.Groups["reminder"];
      if (reminderGroup.Success && reminderGroup.Value.Length > 0)
      {
        reminder = new Parenthetical { Text = reminderGroup.Value };
      }
    }
    else
    {
      // Non-mana form: "Buyback—Sacrifice a land." Delegate the cost text to the shared
      // sacrifice-cost primitive (same one an activation cost "Sacrifice a land" uses).
      var nonManaMatch = _nonManaPattern.Match(clause.RawText);
      if (!nonManaMatch.Success)
      {
        return null;
      }

      var costText = nonManaMatch.Groups["cost"].Value.Trim();
      var (quantity, filter) = ActivatedRuleHelpers.ParseSacrificePattern(costText);
      if (filter == null)
      {
        return null;
      }
      cost = new SacrificeCost { Filter = filter, Quantity = quantity };

      var reminderGroup = nonManaMatch.Groups["reminder"];
      if (reminderGroup.Success && reminderGroup.Value.Length > 0)
      {
        reminder = new Parenthetical { Text = reminderGroup.Value };
      }
    }

    // Ability 1 (CR 702.27a, first clause): the additional-cast-cost static ability.
    // "You may pay an additional [cost] as you cast this spell." Optional ("you may"),
    // payable at most once (not repeatable). The synthesized cost carries no SourceSpan —
    // identity rides on this ability's KeywordSource. The reminder text rides on the
    // primary ability (matching the combinator path).
    var costAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Buyback,
      Reminder = reminder,
      Effects =
      [
        new AdditionalCastCostEffect
        {
          AdditionalCost = new AdditionalCost
          {
            Cost = cost,
            IsOptional = true,
          },
        },
      ],
    };

    // Ability 2 (CR 702.27a, second clause): the return-instead replacement, gated on the
    // buyback cost having been paid. "If the buyback cost was paid, put this spell into its
    // owner's hand instead of into that player's graveyard as it resolves." Modeled as a
    // conditioned static ability carrying a zone-change replacement effect (CR 614): the
    // replaced event is the spell going from the stack to the graveyard; OriginalEventOccurs
    // = false expresses "instead"; the replacement action returns the spell to its owner's
    // hand. Timing ("as it resolves") is the natural clock of the zone-change event, not
    // baked into the effect discriminator.
    var returnInsteadAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Buyback,
      Condition = new KeywordCostPaidCondition { Keyword = KeywordAbility.Buyback },
      Effects =
      [
        new ReplacementEffect
        {
          Event = new ZoneChangeEvent
          {
            OriginZone = Zone.Stack,
            DestinationZone = Zone.Graveyard,
          },
          OriginalEventOccurs = false,
          Replacement = new ReturnToHandEffect { Target = ObjectReference.Self() },
        },
      ],
    };

    return [costAbility, returnInsteadAbility];
  }
}
