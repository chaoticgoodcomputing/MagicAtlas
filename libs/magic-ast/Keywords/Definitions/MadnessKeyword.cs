namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Madness {cost} (CR 702.35a/702.35b): "Madness [cost]" represents two abilities.
/// The first is a static ability: "If a player would discard this card, that player
/// discards it, but exiles it instead of putting it into their graveyard." The second
/// is a triggered ability: "When this card is exiled this way, its owner may cast it
/// by paying [cost] rather than paying its mana cost." (CR 702.35a verbatim.)
/// Casting uses rules for alternative costs (CR 702.35b → 601.2b, 601.2f-h).
///
/// <para>
/// MAST describes, does not execute. The describe axis MAST keeps is the permission to
/// cast from exile at an alternative cost, modeled by the shared
/// <see cref="AlternativeCastEffect"/> primitive (<c>FromZone = Exile</c>). The
/// discard-into-exile replacement event and the "when exiled this way" triggered wrapper
/// are engine territory (ADR-0003 describe-not-execute). No <c>Condition</c> — Madness
/// is unconditional once the discard occurs.
/// </para>
///
/// <para>
/// Combinator-only keyword: Madness has no entry in <c>KeywordDefinitions.All</c>
/// (it is not registered as a <see cref="KeywordDefinition"/>), so
/// <see cref="Definition"/> is <see langword="null"/>.
/// </para>
/// </summary>
[Keyword]
public sealed class MadnessKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Madness")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Madness,
      Effects = [new AlternativeCastEffect
      {
        FromZone = Zone.Exile,
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
