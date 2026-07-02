namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Ninjutsu {cost}: "[Cost], Return an unblocked attacker you control to hand: Put this card
/// onto the battlefield from your hand tapped and attacking."
/// Rule 702.49. MAST records the keyword and the ninjutsu cost; the return-attacker and
/// enter-tapped-and-attacking semantics are engine territory.
///
/// <para>
/// Combinator-only keyword (no <see cref="KeywordDefinition"/>): Ninjutsu has no
/// <c>KeywordDefinitions.Ninjutsu</c> entry in the legacy file; only the parser combinator
/// lived in <c>OracleParsers</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class NinjutsuKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Ninjutsu")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Ninjutsu,
      Effects = [new NinjutsuEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
