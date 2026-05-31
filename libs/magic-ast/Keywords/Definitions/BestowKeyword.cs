namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Bestow {cost}: If you cast this card for its bestow cost, it's an Aura spell
/// with enchant creature. It becomes a creature again if it's not attached.
/// Rule 702.103. MAST records the keyword and the bestow cost; the alternative-cast,
/// Aura-mode, and unattach semantics are engine territory.
///
/// <para>
/// Combinator-only: no <see cref="KeywordDefinition"/> exists in the legacy
/// <c>KeywordDefinitions.cs</c>. <see cref="Definition"/> returns <c>null</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class BestowKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Bestow")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Bestow",
      Effects = [new BestowEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
