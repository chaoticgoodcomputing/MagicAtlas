namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Effects;

/// <summary>
/// "All creatures get -N/-M until end of turn" (and siblings: "Creatures you
/// control get ...", "Creatures your opponents control get ...", "Attacking/
/// Blocking creatures get ...") appearing as the RESOLUTION EFFECT of an
/// activated ability — e.g. Bone Flute's "{2}, {T}: All creatures get -1/-0
/// until end of turn."
///
/// <para>
/// CR 602.1: "Activated abilities have a cost and an effect. They are written
/// as '[Cost]: [Effect.] [Instructions (if any).]'" — the cost ("{2}, {T}")
/// and the effect ("All creatures get -1/-0 until end of turn") are separate
/// composable nodes; <see cref="ActivatedAbilityParser"/> already splits the
/// cost half off, so this rule only recognises the effect half.
/// </para>
///
/// <para>
/// CR 613.4c: "Layer 7c: Effects and counters that modify power and/or
/// toughness (but don't set power and/or toughness to a specific number or
/// value) are applied." — "get -1/-0 until end of turn" is a layer-7c P/T
/// modification, hence a <see cref="MagicAST.AST.Effects.Modification.ModifyPTEffect"/>
/// built from literal modifiers over an <c>Each</c>-kind subject, not a
/// set-P/T effect.
/// </para>
///
/// <para>
/// This is a NEW, collision-free file rather than an edit to
/// <see cref="ModifyPTEffectRule"/> (whose Shape D only matches the singular
/// "Creatures you control get ..." subject, not "All creatures ...") or to
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.MassAnthemSpellRule"/>:
/// <see cref="IActivatedEffectRule"/>'s <c>Effect? TryMatch(string effectText)</c>
/// is a thin wrap of <see cref="MagicAST.Parsing.Parsers.Spell.ISpellRule"/>'s
/// <c>bool TryMatch(string text, out Effect? effect)</c>, so this rule simply
/// delegates to a stateless <see cref="MagicAST.Parsing.Parsers.Spell.Rules.MassAnthemSpellRule"/>
/// instance, reusing the exact node the spell parser already produces for the
/// identical surface (verified against CowerInFear.json / Charge.json gold).
/// This mirrors the in-repo precedent in
/// <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.MassModifyPTTriggeredRule"/>,
/// which delegates the same way for the triggered-ability surface.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 987)]
public sealed class MassModifyPTActivatedEffectRule : IActivatedEffectRule
{
  private static readonly Spell.Rules.MassAnthemSpellRule _mass = new();

  public Effect? TryMatch(string effectText)
  {
    // MassAnthemSpellRule's pattern is anchored with a trailing `$` and expects
    // no sentence-terminal period (mirrors how SpellAbilityParser pre-trims
    // before registry dispatch); ActivatedAbilityParser's registry dispatch does
    // not guarantee that trim, so do it here — same normalisation ModifyPTEffectRule
    // performs locally for the same reason.
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    return _mass.TryMatch(trimmed, out var effect) ? effect : null;
  }
}
