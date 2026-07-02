namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Flash: You may cast this spell any time you could cast an instant.
/// Rule 702.8. Combinator-only keyword — no <see cref="KeywordDefinition"/> exists in
/// the legacy <c>KeywordDefinitions</c> registry.
/// </summary>
[Keyword]
public sealed class FlashKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Flash")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Flash,
      Effects = [new TimingModificationEffect
      {
        Modification = TimingModificationType.Grant,
        Timing = TimingWindow.Instant,
      }],
      Reminder = reminder,
    }
  );
}
