namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Bestow (Rule 702.103a): "Bestow represents a static ability that functions in
/// any zone from which you could play the card it's on. \"Bestow [cost]\" means
/// \"As you cast this spell, you may choose to cast it bestowed. If you do, you pay
/// [cost] rather than its mana cost.\" Casting a spell using its bestow ability
/// follows the rules for paying alternative costs." Rule 702.103b: a spell cast
/// bestowed "becomes an Aura enchantment and gains enchant creature."
///
/// <para>
/// Bestow decomposes across two surfaces. The bestow <em>cost</em> is an
/// alternative casting cost — it lives on the card's
/// <see cref="MagicAST.AST.AlternativeCostsAttribute"/> (emitted by
/// <c>AttributeExtractor</c>), not in an effect. This static ability carries only
/// the becomes-Aura / gains-enchant-creature <em>mode</em> (702.103b), expressed
/// with the shared <see cref="EnchantRestrictionEffect"/> enchant-creature
/// primitive. The alternative-cast resolution, Aura-mode toggle, and unattach
/// rules are engine territory (descriptive-not-executive doctrine).
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
      KeywordSource = KeywordAbility.Bestow,
      Effects = [new EnchantRestrictionEffect
      {
        LegalTargets = new ObjectFilter { CardTypes = ["creature"] },
      }],
      Reminder = reminder,
    }
  );
}
