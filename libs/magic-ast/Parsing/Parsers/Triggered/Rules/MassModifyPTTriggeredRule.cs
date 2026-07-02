namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Effects;

/// <summary>
/// "creatures your opponents control get -N/-M until end of turn" (and siblings:
/// "All creatures get ...", "Creatures you control get ...", "Attacking/Blocking
/// creatures get ...") appearing as the RESOLUTION EFFECT of a triggered ability —
/// e.g. Plague Mare's "When this creature enters, creatures your opponents control
/// get -1/-1 until end of turn."
///
/// <para>
/// CR 603.1: "Triggered abilities have a trigger condition and an effect. They are
/// written as '[When/Whenever/At] [trigger condition or event], [effect]. ...'" —
/// the timing (When this creature enters) and the effect (creatures your opponents
/// control get -1/-1 until end of turn) are separate composable nodes; this rule
/// only recognises the effect half, which <see cref="TriggeredAbilityParser"/>
/// pairs with the already-parsing "When ... enters" trigger.
/// </para>
///
/// <para>
/// CR 613.4c: "Layer 7c: Effects and counters that modify power and/or toughness
/// (but don't set power and/or toughness to a specific number or value) are
/// applied." — "get -1/-1 until end of turn" is a layer-7c P/T modification, hence
/// a <see cref="MagicAST.AST.Effects.Modification.ModifyPTEffect"/> built from
/// literal -1/-1 modifiers, not a set-P/T effect.
/// </para>
///
/// <para>
/// This is a NEW, collision-free file rather than an edit to
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.MassAnthemSpellRule"/> or to
/// <see cref="TriggeredAbilityParser"/>: <see cref="ITriggeredRule"/> and
/// <see cref="MagicAST.Parsing.Parsers.Spell.ISpellRule"/> share the identical
/// <c>bool TryMatch(string text, out Effect? effect)</c> signature, so this rule
/// simply delegates to a stateless <see cref="MagicAST.Parsing.Parsers.Spell.Rules.MassAnthemSpellRule"/>
/// instance, reusing the exact node the spell parser already produces for the
/// identical surface (verified against MakeObsolete.json gold). This mirrors the
/// in-repo precedent in <see cref="TriggeredAbilityParser"/>.ParseEffects, where the
/// dispatcher reuses the spell composite rules
/// <c>ModifyPTAndGainKeyword[Controlled]SpellRule</c> for the triggered "gets +N/+M
/// and gains &lt;keyword&gt;" surface.
/// </para>
///
/// <para>
/// No shadowing: <see cref="ModifyPTTriggeredRule"/> guards on subjects
/// "it"/"this creature"/"target creature" and returns false for "creatures your
/// opponents control" (and the other mass subjects here), so the two rules match
/// disjoint subjects.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class MassModifyPTTriggeredRule : ITriggeredRule
{
  private static readonly Spell.Rules.MassAnthemSpellRule _mass = new();

  public bool TryMatch(string text, out Effect? effect) => _mass.TryMatch(text, out effect);
}
