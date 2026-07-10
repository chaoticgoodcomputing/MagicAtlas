namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

/// <summary>
/// "Spells you cast that share a card type with the exiled card cost {N} less to cast." —
/// a cost-reduction static ability whose affected set is filtered by a relational card-type
/// overlap with the linked Imprint card (Semblance Anvil, CR 118.7 cost reduction).
///
/// <para>This is the consumer half of a CR 406.6 linked-ability pair: the producer is the
/// Imprint ETB exile (<see cref="MagicAST.Parsing.Parsers.Triggered.Rules.ExileNonlandCardFromHandImprintTriggeredRule"/>).
/// "the exiled card" is NOT free text — it is the linked exiled reference (ADR 0004
/// "reference not resolution"), modelled as an <see cref="ObjectReferenceKind.Any"/>
/// reference with Zone.Exile + ExiledWith: Self, the Isochron Scepter precedent
/// (<see cref="MagicAST.Parsing.Parsers.Activated.Rules.CopyExiledCardAndCastWithoutPayingEffectRule"/>).
/// "share a card type with" is the relational <see cref="ObjectFilter.SharesCardTypeWith"/>
/// axis (CR 110.4), parallel to <see cref="ObjectFilter.SharesCreatureTypeWith"/> (CR
/// 205.3): the card types to match are those the exiled card CURRENTLY has, resolved by a
/// consumer, not a literal type list.</para>
///
/// <para>Distinct from <see cref="ChosenTypeSpellCostReductionRule"/> (a chosen-type
/// linked ability, CR 607, not an exiled-card relational overlap) and from
/// <see cref="TypeSpellCostReductionRule"/> (a fixed printed card type). The pattern
/// anchors on "share a card type with the exiled card" to distinguish it from every
/// sibling cost-reduction shape.</para>
/// </summary>
[StaticRule(Priority = 986)]
public sealed class SharesCardTypeSpellCostReductionRule : IStaticRule
{
  // "Spells you cast that share a card type with the exiled card cost {N} less to cast."
  private static readonly Regex Pattern = new(
    @"^\s*Spells\s+you\s+cast\s+that\s+share\s+a\s+card\s+type\s+with\s+the\s+exiled\s+card\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = Pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var amount = int.Parse(match.Groups["amount"].Value);

    var exiledCard = new ObjectReference
    {
      Kind = ObjectReferenceKind.Any,
      Filter = new ObjectFilter
      {
        Zone = Zone.Exile,
        ExiledWith = new ObjectReference { Kind = ObjectReferenceKind.Self },
      },
    };

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Resource.CostReductionEffect
        {
          Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
        }],
        AffectedObjects = new ObjectFilter
        {
          CardTypes = ["spell"],
          Controller = ControllerFilter.You,
          SharesCardTypeWith = exiledCard,
        },
      },
    ];
  }
}
