namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Parses "Attacking &lt;creatures|tokens&gt; you control have [keyword]." — a static
/// continuous effect (CR 604.1: "Static abilities do something all the time rather
/// than being activated or triggered. They are written as statements, and they're
/// simply true.") granting a keyword ability to the attacking subset of the
/// controller's creatures/tokens. This is Starry-Eyed Skyrider's third line:
/// "Attacking tokens you control have flying."
///
/// <para>
/// CRITICAL: "Attacking" here is a COMBAT STATE (CR 508; glossary "attacking
/// creature": "A creature that has either been declared as part of a legal attack
/// during the combat phase … It remains an attacking creature until it's removed
/// from combat or the combat phase ends"), NOT a creature subtype (CR 205.3m).
/// It is therefore encoded as a <see cref="CombatStateCharacteristic"/> with
/// <see cref="CombatState.Attacking"/> on the filter — mirroring Goblin Oriflamme
/// ("Attacking creatures you control get +1/+0") — and never as
/// <c>Subtypes = ["Attacking"]</c>.
/// </para>
///
/// <para>
/// The "tokens" qualifier maps to <c>IsToken = true</c> (CR 111.1: "A token is a
/// marker used to represent any permanent that isn't represented by a card."); the
/// bare "creatures" form leaves <c>IsToken</c> unset. Following the bare-token
/// convention of <see cref="SubtypeTokensHaveKeywordRule"/>, no <c>CardTypes</c> is
/// emitted for "tokens" (the text says "tokens", not "creature tokens"). "you
/// control" is <c>Controller = You</c> (CR 109.5). The keyword resolves through
/// <see cref="StaticRuleHelpers.MapKeywordToStaticAbility"/> (CR 702.9a/702.9b for
/// flying); an unrecognised keyword declines so no free text is emitted.
/// </para>
///
/// <para>
/// Priority 972 — ABOVE <see cref="SubtypeTokensHaveKeywordRule"/> (970) so this
/// more-specific combat-state shape intercepts the mis-parse in which the generic
/// "&lt;Subtype&gt; tokens you control have …" rule would capture "Attacking" as a
/// subtype word. Static dispatch is descending-Priority, first-non-null-wins, so
/// intercepting by priority is sufficient — <see cref="SubtypeTokensHaveKeywordRule"/>
/// is left untouched. The case-sensitive literal "Attacking" and the anchored
/// pattern keep this collision-free.
/// </para>
/// </summary>
[StaticRule(Priority = 972)]
public sealed class AttackingObjectsHaveKeywordRule : IStaticRule
{
  // Case-sensitive "Attacking" (the printed combat-state qualifier), anchored to
  // prevent substring matches. <obj> distinguishes token vs bare-creature grants.
  private static readonly Regex _pattern = new(
    @"^\s*Attacking\s+(?<obj>creatures?|tokens?)\s+you\s+control\s+have\s+(?<kw>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);

    var m = _pattern.Match(rawText);
    if (!m.Success)
    {
      return null;
    }

    var kw = m.Groups["kw"].Value.Trim().ToLowerInvariant();
    var grantedAbility = StaticRuleHelpers.MapKeywordToStaticAbility(kw);
    if (grantedAbility is null)
    {
      return null;
    }

    var isToken = m.Groups["obj"].Value.StartsWith("token", StringComparison.OrdinalIgnoreCase);

    return
    [
      new StaticAbility
      {
        Effects = [new GainAbilityEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter
            {
              IsToken = isToken ? true : null,
              Controller = ControllerFilter.You,
              Characteristics = [Characteristic.InCombat(CombatState.Attacking)],
            },
          },
          GainedAbility = grantedAbility,
        }],
      },
    ];
  }
}
