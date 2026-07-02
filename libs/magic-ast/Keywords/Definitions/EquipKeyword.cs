namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Equip [cost]: Attach to target creature you control. Activate only as a sorcery.
///
/// CR 702.6a (verbatim): "Equip is an activated ability of Equipment cards. 'Equip
/// [cost]' means '[Cost]: Attach this permanent to target creature you control.
/// Activate only as a sorcery.'"
///
/// CR 702.6c: "Equip [quality]" restricts legal targets to a creature with the chosen
/// quality controlled by the activating player.
///
/// CR 702.6e: "Equip planeswalker [cost]" attaches to target planeswalker you control.
///
/// Combinator-only: no KeywordDefinition entry in the legacy registry.
/// </summary>
[Keyword]
public sealed class EquipKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Equip")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new ActivatedAbility
    {
      KeywordSource = KeywordAbility.Equip,
      Costs = [cost],
      Effects =
      [
        new AttachEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Controller = ControllerFilter.You,
            },
          },
        },
      ],
      Restrictions = [ActivationRestriction.OnlyAsSorcery],
      IsManaAbility = false,
      Reminder = reminder,
    }
  );
}
