namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Scavenge {cost}: "{Cost}, Exile this card from your graveyard: Put a number of +1/+1
/// counters equal to this card's power on target creature. Scavenge only as a sorcery."
/// Rule 702.97. An activated ability playable only from the graveyard. MAST records the
/// keyword and its associated mana cost; the counter-placement and timing restriction are
/// inferred from the rules. Combinator-only: no matching <c>KeywordDefinitions</c> entry
/// exists in the legacy registry.
/// </summary>
[Keyword]
public sealed class ScavengeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Scavenge")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Scavenge",
      Effects = [new ScavengeEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
