namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Cleave [cost] (CR 702.148a): "Cleave is a keyword that represents two static
/// abilities that function while a spell with cleave is on the stack. 'Cleave
/// [cost]' means 'You may cast this spell by paying [cost] rather than paying its
/// mana cost' and 'If this spell's cleave cost was paid, change its text by
/// removing all text found within square brackets in the spell's rules text.'
/// Casting a spell for its cleave cost follows the rules for paying alternative
/// costs in rules 601.2b and 601.2f-h."
///
/// <para>
/// It is a static ability, so the combinator emits a <see cref="StaticAbility"/>
/// carrying the shared <see cref="AlternativeCastEffect"/> primitive that
/// Flashback/Surge/Escape etc. also use (<c>FromZone = Hand</c> — cleave does not
/// change the legal casting zone, only the cost — <c>Cost = </c> the cleave cost).
/// Mirrors <see cref="FlashbackKeyword"/> exactly, differing only in
/// <see cref="AlternativeCastEffect.FromZone"/>.
/// </para>
///
/// <para>
/// The second static ability (the square-bracket text-removal rewrite) is engine
/// territory, not a descriptive axis of the card — MAST records only the
/// keyword's presence and the alternative cost, mirroring how
/// <see cref="OverloadKeyword"/> treats its own text-rewrite half ("target" →
/// "each") as unmodeled. The printed square-bracket markup itself remains part
/// of the card's other oracle lines and is parsed as printed.
/// </para>
///
/// Combinator-only keyword — no <see cref="KeywordDefinition"/> exists in the
/// legacy <c>KeywordDefinitions</c> registry.
/// </summary>
[Keyword]
public sealed class CleaveKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Cleave")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Cleave,
      Effects = [new AlternativeCastEffect
      {
        FromZone = Zone.Hand,
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );
}
