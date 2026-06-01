namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Target attacking creature gets [+-](N|X)/[+-](M|X) until end of turn." — the
/// from-hand bloodrush pump (e.g. Rubblebelt Maaka, "Bloodrush — {R}, Discard this
/// card: Target attacking creature gets +3/+3 until end of turn.").
///
/// <para>
/// Distinct from <see cref="ModifyPTEffectRule"/>'s "Target creature gets …" shape:
/// the target is narrowed to an <em>attacking</em> creature, carried as the
/// state-predicate residual <c>Characteristic.Other("attacking")</c> per ADR 0001
/// (no first-class ObjectFilter field for combat state) — mirroring how
/// "Attacking creatures get …" (Goblin Oriflamme) and "Destroy target attacking
/// creature" (Immolating Glare) already encode the attacking filter.
/// </para>
///
/// <para>
/// "Bloodrush" itself is an ability word with no rules meaning (CR 207.2c —
/// bloodrush is explicitly listed among the ability words); the classifier captures
/// it onto the activated ability's <c>AbilityWord</c> label. The P/T modifier is a
/// layer-7c effect (CR 613.4c) — it adds to power/toughness without setting them.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 988)]
public sealed class ModifyPTTargetAttackingCreatureEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();

    // One modifier side: [+\-](\d+|X) e.g. "+3", "-2", "+X".
    const string modGrammar = @"(?<{0}>[+\-](?:\d+|X))";
    var pGroup = string.Format(modGrammar, "p");
    var tGroup = string.Format(modGrammar, "t");

    var m = Regex.Match(
      trimmed,
      $@"^Target\s+attacking\s+creature\s+gets\s+{pGroup}/{tGroup}\s+until\s+end\s+of\s+turn$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    return new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = [Characteristic.Other("attacking")],
        },
      },
      PowerModifier = ActivatedRuleHelpers.ParseSignedModifier(m.Groups["p"].Value),
      ToughnessModifier = ActivatedRuleHelpers.ParseSignedModifier(m.Groups["t"].Value),
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
