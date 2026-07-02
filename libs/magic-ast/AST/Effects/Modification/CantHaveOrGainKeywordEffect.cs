namespace MagicAST.AST.Effects.Modification;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[target] can't have or gain [keyword]" — the can't-have lock of CR 113.11.
///
/// <para>
/// This single node captures BOTH halves of the Archetype-cycle opponent lock
/// ("Creatures your opponents control lose flying and can't have or gain flying.")
/// as ONE continuous effect, not two. Per CR 113.11: "Effects can stop an object
/// from having a specified ability. These effects say that the object 'can't have'
/// that ability. If the object has that ability, it loses it. It's also impossible
/// for an effect or keyword counter to add that ability to the object." The removal
/// ("lose flying") is therefore SUBSUMED by the can't-have construct — modelling it
/// with a separate <see cref="LoseAbilityEffect"/> would double-count what CR 113.11
/// already defines as one effect. CR 611.3 is the static-ability continuous-effect
/// authority that generates this lock.
/// </para>
///
/// <para>
/// The locked keyword is carried by the structured <see cref="AST.References.KeywordAbility"/>
/// enum — never a free-text string — so this node is gate-clean and casing-proof
/// (mirrors <c>KeywordAbilityEffect.Keyword</c>). Derives from
/// <see cref="ContinuousEffect"/> (CR 611): the lock persists.
/// </para>
/// </summary>
[OracleEffect("cantHaveOrGainKeyword")]
public sealed record CantHaveOrGainKeywordEffect : ContinuousEffect
{
  /// <summary>The objects the keyword-lock applies to.</summary>
  public required ObjectReference Target { get; init; }

  /// <summary>The keyword the target can't have or gain (and loses if it has it).</summary>
  public required KeywordAbility Keyword { get; init; }
}
