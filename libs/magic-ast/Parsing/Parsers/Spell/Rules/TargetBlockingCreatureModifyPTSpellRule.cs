namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target blocking creature gets +N/+M until end of turn." (and the "-N/-M" sign
/// form) — a combat-trick pump narrowed to a creature that is currently blocking.
///
/// <para>
/// Distinct from <see cref="ModifyPTSpellRule"/>'s bare "Target creature gets …"
/// shape: the target is narrowed to a <em>blocking</em> creature, carried as the
/// <see cref="Characteristic.InCombat(CombatState)"/> state-predicate residual per
/// the established open-ended-qualifier convention (CR 109.3 — characteristics
/// include any quality that can be true or false of a permanent at any moment) —
/// mirroring how "Destroy target blocking creature" (<see cref="DestroyTargetStateQualifiedRule"/>)
/// and "Target attacking creature gets …" (<see cref="MagicAST.Parsing.Parsers.Activated.Rules.ModifyPTTargetAttackingCreatureEffectRule"/>,
/// bloodrush) already encode the combat-state filter.
/// </para>
///
/// <para>
/// The P/T modifier is a layer-7c effect (CR 613.4c — "Effects and counters that
/// modify power and/or toughness (but don't set power and/or toughness to a
/// specific number or value) are applied.").
/// </para>
///
/// <list type="bullet">
///   <item>"Target blocking creature gets +3/+1 until end of turn." (Aliban's Tower)</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class TargetBlockingCreatureModifyPTSpellRule : ISpellRule
{
  // One modifier side: [+\-]\d+ e.g. "+3", "-2".
  private const string ModGrammar = @"(?<{0}>[+\-]\d+)";

  private static readonly Regex Pattern = new(
    $@"^Target\s+blocking\s+creature\s+gets\s+{string.Format(ModGrammar, "p")}/{string.Format(ModGrammar, "t")}\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var power = int.Parse(m.Groups["p"].Value);
    var toughness = int.Parse(m.Groups["t"].Value);

    effect = new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = [Characteristic.InCombat(CombatState.Blocking)],
        },
      },
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
