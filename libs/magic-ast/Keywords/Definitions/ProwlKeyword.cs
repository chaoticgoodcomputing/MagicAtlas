namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Prowl [cost] — CR 702.76a: "Prowl is a static ability that functions on the
/// stack. 'Prowl [cost]' means 'You may pay [cost] rather than pay this spell's
/// mana cost if a player was dealt combat damage this turn by a source that, at
/// the time it dealt that damage, was under your control and had any of the
/// creature types of this spell.'"
///
/// <para>
/// Emits a <see cref="StaticAbility"/> carrying an <see cref="AlternativeCastEffect"/>
/// with <c>FromZone = Zone.Hand</c> and a hard-coded <see cref="OtherCondition"/>
/// residual carrying the verbatim CR 702.76a gate text — the same shape as its
/// sibling cast-from-hand conditional keywords Surge, Spectacle, and Freerunning
/// (the latter is Prowl's direct successor mechanic, differing only in whose
/// creature types gate the permission).
/// </para>
/// </summary>
[Keyword]
public sealed class ProwlKeyword : IKeyword
{
  private const string ConditionText =
    "a player was dealt combat damage this turn by a source that, at the time it dealt that damage, was under your control and had any of the creature types of this spell";

  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Prowl")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Prowl,
      Effects =
      [
        new AlternativeCastEffect
        {
          FromZone = Zone.Hand,
          Cost = cost,
          Condition = new OtherCondition { Text = ConditionText },
        },
      ],
      Reminder = reminder,
    }
  );
}
