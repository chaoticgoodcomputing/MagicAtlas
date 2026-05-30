namespace MagicAST.AST.Effects.Keyword;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A parameterless keyword ability (CR 702) carried by its identity alone —
/// deathtouch, trample, vigilance, cascade, persist, … The single node every such
/// marker collapses into (ADR 0006): the <see cref="KeywordAbility"/> enum value is
/// the whole content, so the 67 former one-per-keyword empty records become one.
///
/// <para>
/// The same enum identifies a keyword wherever it appears — this effect, a granted
/// ability (<c>GainAbilityEffect.GainedAbility</c>), a characteristic filter
/// (<c>KeywordCharacteristic</c>), an ability-class filter — one source of truth.
/// Carries no trait fields: a temporary grant ("gains trample until end of turn")
/// is a <c>GainAbilityEffect</c> whose <c>Duration</c> holds the span, not this node.
/// </para>
/// </summary>
[OracleEffect("keywordAbility")]
public sealed record KeywordAbilityEffect : Effect
{
  /// <summary>The keyword ability's canonical identity.</summary>
  public required KeywordAbility Keyword { get; init; }
}
