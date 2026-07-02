namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Target attacking creature gets &lt;±P/±T&gt; and gains &lt;kw&gt; until end of
/// turn" — the from-hand bloodrush pump+grant (e.g. Wasteland Viper, "Bloodrush —
/// {G}, Discard this card: Target attacking creature gets +1/+2 and gains
/// deathtouch until end of turn.").
///
/// <para>
/// Splices <see cref="ModifyPTTargetAttackingCreatureEffectRule"/>'s target/filter
/// half (attacking-creature target form) with
/// <see cref="GetsPTAndGainsKeywordEffectRule"/>'s pump+grant composite shape (which
/// is anchored to the "This creature" self form). Neither existing rule alone
/// handles the TARGET-form pump+grant sentence.
/// </para>
///
/// <para>
/// <b>CR 613.4c</b> (P/T modifier clause): "Layer 7c: Effects and counters that
/// modify power and/or toughness (but don't set power and/or toughness to a specific
/// number or value) are applied." The granted keyword is a continuous ability grant
/// that applies in layer 6 (ability-adding effects). Both clauses are present in
/// oracle text; this parser describes them without encoding layer machinery.
/// </para>
///
/// <para>
/// "Bloodrush" itself is an ability word with no rules meaning (<b>CR 207.2c</b>:
/// "An ability word appears in italics at the beginning of some abilities. Ability
/// words are similar to keywords in that they tie together cards that have similar
/// functionality, but they have no special rules meaning and no individual entries
/// in the Comprehensive Rules. The ability words are adamant, addendum, alliance,
/// battalion, bloodrush, ..."); the classifier captures it onto the activated
/// ability's <c>AbilityWord</c> label.
/// </para>
///
/// <para>
/// <b>CR 702.2a</b> ("Deathtouch"): "Deathtouch is a static ability."
/// </para>
///
/// <para>
/// Returns a <see cref="CompositeEffect"/> whose child list carries, in order:
/// <list type="number">
///   <item>A <see cref="ModifyPTEffect"/> targeting an attacking creature with EOT
///         duration.</item>
///   <item>A <see cref="GainAbilityEffect"/> targeting <see cref="ObjectReferenceKind.It"/>
///         (anaphora back to the targeted attacking creature — NOT
///         <see cref="ObjectReferenceKind.Self"/>, since the pumped creature is the
///         one that gains the keyword, not the source permanent), the keyword
///         ability, and EOT duration.</item>
/// </list>
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 997)]
public sealed class TargetAttackingCreatureGetsPTAndGainsKeywordEffectRule : IActivatedEffectRule
{
  // Token grammar: [+\-](\d+|X) — e.g. "+1", "-2", "+X", "-X".
  private const string ModGrammar = @"(?<{0}>[+\-](?:\d+|X))";

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();

    var pGroup = string.Format(ModGrammar, "p");
    var tGroup = string.Format(ModGrammar, "t");

    // "Target attacking creature gets <±P/±T> and gains <kw> until end of turn"
    var match = Regex.Match(
      trimmed,
      $@"^Target\s+attacking\s+creature\s+gets\s+{pGroup}/{tGroup}\s+and\s+gains?\s+(?<kw>[a-z]+(?:\s+(?!until|for|as\b)[a-z]+)?)\s+until\s+end\s+of\s+turn$",
      RegexOptions.IgnoreCase
    );

    if (!match.Success)
    {
      return null;
    }

    var keywordRaw = match.Groups["kw"].Value.ToLowerInvariant().Trim();
    var grantedAbility = ActivatedRuleHelpers.BuildGrantedKeywordAbility(keywordRaw);
    if (grantedAbility is null)
    {
      // Keyword not yet modelled; fall through so the fallback can surface this
      // as unparsed rather than silently eating the text.
      return null;
    }

    var eot = UntilTimeDuration.EndOfTurn;

    var modifyPT = new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = [Characteristic.InCombat(CombatState.Attacking)],
        },
      },
      PowerModifier = ActivatedRuleHelpers.ParseSignedModifier(match.Groups["p"].Value),
      ToughnessModifier = ActivatedRuleHelpers.ParseSignedModifier(match.Groups["t"].Value),
      Duration = eot,
    };

    var gainAbility = new GainAbilityEffect
    {
      Target = ObjectReference.It(),
      GainedAbility = grantedAbility,
      Duration = eot,
    };

    return new CompositeEffect
    {
      Effects = [modifyPT, gainAbility],
    };
  }
}
