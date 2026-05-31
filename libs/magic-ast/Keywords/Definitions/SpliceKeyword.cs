namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Splice onto [subtype] {cost}: while this card is in hand, you may reveal it as
/// you cast a spell of the named subtype and pay the splice cost to graft this
/// card's instructions onto that spell. Rule 702.47.
///
/// <para>
/// MAST records the keyword, the spell subtype a spell must share to be a legal
/// splice target (printed verbatim after "onto"), and the splice cost. The
/// text-grafting machinery (reveal, copy instructions onto the target spell) is
/// reminder text and is conventionally inferred from the rules — not modeled.
/// Combinator-only keyword: no <see cref="KeywordDefinition"/> exists in the
/// legacy <c>KeywordDefinitions</c> registry.
/// </para>
/// </summary>
[Keyword]
public sealed class SpliceKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <summary>
  /// Parses the spell subtype printed after "onto" (e.g. "Arcane") as a bare
  /// word, capturing its text. Private to this keyword: the "Splice onto [subtype]"
  /// shape is unique to this combinator.
  /// </summary>
  private static readonly TokenListParser<OracleToken, string> SubtypeWord = Token
    .EqualTo(OracleToken.Word)
    .Select(t => t.ToStringValue());

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Splice")
    from onto in Keyword("onto")
    from subtype in SubtypeWord
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Splice",
      Effects = [new SpliceEffect
      {
        Onto = new ObjectFilter { Subtypes = [subtype] },
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
