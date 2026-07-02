namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Equip [quality] [cost]: Attach to target creature with the chosen quality you
/// control. Activate only as a sorcery.
///
/// CR 702.6a (verbatim): "Equip is an activated ability of Equipment cards. 'Equip
/// [cost]' means '[Cost]: Attach this permanent to target creature you control.
/// Activate only as a sorcery.'"
///
/// CR 702.6c (verbatim): "Equip abilities may further restrict what creatures may
/// be chosen as legal targets. Such restrictions usually appear in the form 'Equip
/// [quality]' or 'Equip [quality] creature.' These equip abilities may legally
/// target only a creature that's controlled by the player activating the ability
/// and that has the chosen quality. Additional restrictions for an equip ability
/// don't restrict what the Equipment may be attached to."
///
/// CR 702.6d (verbatim): "If a permanent has multiple equip abilities, any of its
/// equip abilities may be activated." — a card may carry both this typed variant
/// and a plain <see cref="EquipKeyword"/>; both are independent activated abilities
/// sharing the same <see cref="KeywordAbility.Equip"/> identity (ADR 0003).
///
/// Combinator-only: no KeywordDefinition entry in the legacy registry.
/// </summary>
[Keyword]
public sealed class EquipQualityKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Equip")
    from quality in Token.EqualTo(OracleToken.Word)
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
              Subtypes = [quality.ToStringValue()],
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
