namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the composite "pump + lure" shape:
///   "Target creature gets +N/+M until end of turn and must be blocked this turn if able."
///   (Compelled Duel)
///
/// Two continuous effects apply to the same single target creature, joined by a bare
/// "and" inside one sentence (no sentence boundary, so the sentence-bundle path never
/// sees it): a P/T modifier (CR 611.2c — a continuous effect from resolution that
/// modifies characteristics) and a block requirement (CR 509.1c — a "must be blocked
/// if able" requirement the defending player must satisfy when declaring blockers).
/// Both last until end of turn ("this turn" for the block requirement is the same
/// clock-bounded expiry).
///
/// Emits a flat list via <see cref="IMultiSpellRule.TryMatchMulti"/>:
/// <c>[ModifyPTEffect, MustBeBlockedEffect]</c>, reusing the exact nodes the
/// single-effect siblings emit (<see cref="ModifyPTSpellRule"/> and
/// <see cref="MustBeBlockedTargetRule"/>) so a single-target pump and a single-target
/// lure compose without a new discriminator.
///
/// <para>
/// The single-effect <see cref="ISpellRule.TryMatch"/> always returns false so the
/// flat-list path is the only active route (mirrors <see cref="ModifyPTAndGainKeywordSpellRule"/>).
/// </para>
/// </summary>
[SpellRule]
public sealed class ModifyPTAndMustBeBlockedSpellRule : ISpellRule, IMultiSpellRule
{
  // Anchored end-to-end so it cannot substring-match a more-specific sibling.
  private static readonly Regex _pattern = new(
    @"^Target\s+creature\s+gets\s+(?<p>[+-]\d+)/(?<t>[+-]\d+)\s+until\s+end\s+of\s+turn\s+and\s+must\s+be\s+blocked\s+this\s+turn\s+if\s+able$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // ISpellRule single-effect path intentionally disabled — dispatch is via TryMatchMulti.
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var power = int.Parse(m.Groups["p"].Value);
    var toughness = int.Parse(m.Groups["t"].Value);
    var duration = UntilTimeDuration.EndOfTurn;

    ObjectReference TargetCreature() =>
      new()
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      };

    effects = new List<Effect>
    {
      new ModifyPTEffect
      {
        Target = TargetCreature(),
        PowerModifier = LiteralQuantity.Of(power),
        ToughnessModifier = LiteralQuantity.Of(toughness),
        Duration = duration,
      },
      new MustBeBlockedEffect
      {
        Target = TargetCreature(),
        Duration = duration,
      },
    };
    return true;
  }
}
