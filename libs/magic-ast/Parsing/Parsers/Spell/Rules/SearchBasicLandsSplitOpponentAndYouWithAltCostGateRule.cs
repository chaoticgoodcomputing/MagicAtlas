namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Search your library for up to four basic land cards and reveal them. Put one
/// of them onto the battlefield tapped under an opponent's control if the {3}{G}
/// cost was paid. Put two of them onto the battlefield tapped under your control
/// and the rest into your hand. Then shuffle." — the Verdant Mastery shape.
///
/// <para>
/// A three-way <see cref="SearchLibraryEffect.Placements"/> distribution (CR
/// 701.23), extending the Cultivate / Kodama's Reach two-way split (see
/// <see cref="SearchLibraryDistributeBasicLandsToBattlefieldAndHandRule"/>) with
/// (a) a THIRD share, and (b) one share gated on whether the card's own
/// alternative cost (CR 118.9, printed as a separate ability — see
/// <see cref="MagicAST.Parsing.Parsers.Static.GrantAlternativeCostForThisSpellRule"/>)
/// was paid — <see cref="MagicAST.AST.Abilities.AlternativeCostPaidCondition"/> —
/// and controlled by an opponent rather than the caster —
/// <see cref="SearchPlacement.Controller"/>. The final share ("the rest") is a
/// <see cref="RemainderQuantity"/>: not a chosen amount, but whatever is left of
/// the found total after the other two shares.
/// </para>
///
/// <para>
/// Fully anchored (<c>^…$</c>) over the ENTIRE multi-sentence clause (this rule
/// implements <see cref="ISpellRule"/>, tried by <c>SpellAbilityParser</c> against
/// the whole clause text once per-sentence bundling and multi-rule dispatch have
/// both declined) so it cannot collide with any other search rule or any
/// standalone "Put N of them..." fragment rule.
/// </para>
/// </summary>
[SpellRule]
public sealed class SearchBasicLandsSplitOpponentAndYouWithAltCostGateRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Search\s+your\s+library\s+for\s+up\s+to\s+(?<search>[a-z]+)\s+basic\s+land\s+cards\s+and\s+reveal\s+them\.\s+"
    + @"Put\s+(?<opp>[a-z]+)\s+of\s+them\s+onto\s+the\s+battlefield\s+tapped\s+under\s+an\s+opponent's\s+control\s+if\s+the\s+(?:\{[^}]+\})+\s+cost\s+was\s+paid\.\s+"
    + @"Put\s+(?<you>[a-z]+)\s+of\s+them\s+onto\s+the\s+battlefield\s+tapped\s+under\s+your\s+control\s+and\s+the\s+rest\s+into\s+your\s+hand\.\s+"
    + @"Then\s+shuffle$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly ObjectFilter _basicLandFilter = new()
  {
    Supertypes = ["Basic"],
    CardTypes = ["land"],
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var match = _pattern.Match(text.Trim().TrimEnd('.'));
    if (!match.Success)
    {
      return false;
    }

    if (!SpellRuleHelpers.TryParseSmallWord(match.Groups["search"].Value, out var searchMax))
    {
      return false;
    }
    if (!SpellRuleHelpers.TryParseSmallWord(match.Groups["opp"].Value, out var oppCount))
    {
      return false;
    }
    if (!SpellRuleHelpers.TryParseSmallWord(match.Groups["you"].Value, out var yourCount))
    {
      return false;
    }

    effect = new SearchLibraryEffect
    {
      Filter = _basicLandFilter,
      Count = new UpToQuantity { Maximum = searchMax, Minimum = 0 },
      Destination = SearchDestination.Distributed,
      Revealed = true,
      Placements =
      [
        new SearchPlacement
        {
          Count = LiteralQuantity.Of(oppCount),
          Destination = SearchDestination.BattlefieldTapped,
          Controller = ControllerFilter.Opponent,
          Condition = new AlternativeCostPaidCondition(),
        },
        new SearchPlacement
        {
          Count = LiteralQuantity.Of(yourCount),
          Destination = SearchDestination.BattlefieldTapped,
          Controller = ControllerFilter.You,
        },
        new SearchPlacement
        {
          Count = new RemainderQuantity(),
          Destination = SearchDestination.Hand,
        },
      ],
    };
    return true;
  }
}
