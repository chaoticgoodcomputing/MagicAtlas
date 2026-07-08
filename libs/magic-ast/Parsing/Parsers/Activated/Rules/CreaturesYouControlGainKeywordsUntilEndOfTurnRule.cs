namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Creatures you control gain [keyword] [and [keyword] ...] until end of turn"
/// (The Wind Crystal: "Creatures you control gain flying and lifelink until end of
/// turn."). One or more keyword names joined by " and ", all with until-end-of-turn
/// duration, granted to every creature you control.
///
/// <para>
/// Mirrors <see cref="MagicAST.Parsing.Parsers.Spell.Rules.ModifyPTAndGainKeywordSpellRule"/>'s
/// keyword-splitting shape at the activated-ability layer: emits one
/// <see cref="GainAbilityEffect"/> per keyword via
/// <see cref="Activated.IMultiActivatedEffectRule.TryMatchMulti"/> rather than
/// collapsing the "and"-joined list into a single grant (which would silently drop
/// every keyword but the first — the single-effect
/// <see cref="GainAbilityEffectRule"/>'s "Creatures you control gain (\w+)" branch
/// only captures one word). Reuses
/// <see cref="ActivatedRuleHelpers.BuildGrantedKeywordAbility"/> for the per-keyword
/// <c>StaticAbility</c> (CR 113.6/113.10 — a granted ability is a full-fledged
/// ability of the gainer), so a single-keyword card (e.g. Vito, Thorn of the Dusk
/// Rose's "Creatures you control gain lifelink until end of turn") produces the
/// identical shape it already did via the single-effect path.
/// </para>
///
/// <para>
/// The single-effect <see cref="IActivatedEffectRule.TryMatch"/> always returns null
/// so the flat-list path via <see cref="TryMatchMulti"/> is the only active route.
/// Anchored (^…$).
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 996)]
public sealed class CreaturesYouControlGainKeywordsUntilEndOfTurnRule
  : IActivatedEffectRule,
    IMultiActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Creatures\s+you\s+control\s+gain\s+(?<kws>.+?)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var match = _pattern.Match(effectText.Trim().TrimEnd('.'));
    if (!match.Success)
    {
      return false;
    }

    var duration = UntilTimeDuration.EndOfTurn;
    var target = new ObjectReference
    {
      Kind = ObjectReferenceKind.Each,
      Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
    };

    var keywordNames = Regex.Split(match.Groups["kws"].Value.Trim(), @"\s+and\s+", RegexOptions.IgnoreCase);
    var list = new List<Effect>();
    foreach (var name in keywordNames)
    {
      var ability = ActivatedRuleHelpers.BuildGrantedKeywordAbility(name.Trim());
      if (ability is null)
      {
        // Unrecognised keyword — bail so the fallback parser handles the card.
        return false;
      }
      list.Add(new GainAbilityEffect
      {
        Target = target,
        GainedAbility = ability,
        Duration = duration,
      });
    }

    effects = list;
    return true;
  }
}
