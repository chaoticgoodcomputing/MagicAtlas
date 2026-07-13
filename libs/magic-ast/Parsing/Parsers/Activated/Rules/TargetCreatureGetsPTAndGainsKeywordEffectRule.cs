namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Target creature gets &lt;±P/±T&gt; and gains &lt;kw&gt; until end of turn" —
/// a single-sentence TWO-clause activated effect that neither
/// <see cref="ModifyPTEffectRule"/> nor <see cref="GainAbilityEffectRule"/>
/// handles alone. This is the targeted sibling of
/// <see cref="GetsPTAndGainsKeywordEffectRule"/> ("This creature gets …"); here the
/// buff lands on a chosen target creature (e.g. Ixalli's Keeper:
/// "{7}{G}, {T}, Sacrifice this creature: Target creature gets +5/+5 and gains
/// trample until end of turn.").
///
/// <para>
/// <b>CR 613.4c</b> (P/T modifier clause): "Layer 7c: Effects and counters that
/// modify power and/or toughness (but don't set power and/or toughness to a specific
/// number or value) are applied." The granted keyword is a continuous ability grant
/// that applies in layer 6 ("Ability-adding effects, keyword counters, ability-removing
/// effects, and effects that say an object can't have an ability are applied."). Both
/// clauses are present in oracle text; this parser describes them without encoding the
/// layer machinery.
/// </para>
///
/// <para>
/// Returns a <see cref="CompositeEffect"/> whose child list carries, in order:
/// <list type="number">
///   <item>A <see cref="ModifyPTEffect"/> targeting the chosen creature with EOT
///         duration.</item>
///   <item>A <see cref="GainAbilityEffect"/> targeting "it" (the same creature),
///         the keyword ability, and EOT duration.</item>
/// </list>
/// This mirrors the spell-side shape emitted by
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.ModifyPTAndGainKeywordSpellRule"/>.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 996)]
public sealed class TargetCreatureGetsPTAndGainsKeywordEffectRule : IActivatedEffectRule
{
  // Token grammar: [+\-](\d+|X) — e.g. "+1", "-2", "+X", "-X".
  private const string ModGrammar = @"(?<{0}>[+\-](?:\d+|X))";

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();

    var pGroup = string.Format(ModGrammar, "p");
    var tGroup = string.Format(ModGrammar, "t");

    // "Target creature gets <±P/±T> and gains <kw> until end of turn"
    var match = Regex.Match(
      trimmed,
      $@"^Target\s+creature\s+gets\s+{pGroup}/{tGroup}\s+and\s+gains?\s+(?<kw>[a-z]+(?:\s+(?!until|for|as\b)[a-z]+)?)\s+until\s+end\s+of\s+turn$",
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
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      PowerModifier = ActivatedRuleHelpers.ParseSignedModifier(match.Groups["p"].Value),
      ToughnessModifier = ActivatedRuleHelpers.ParseSignedModifier(match.Groups["t"].Value),
      Duration = eot,
    };

    var gainAbility = new GainAbilityEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.It },
      GainedAbility = grantedAbility,
      Duration = eot,
    };

    return new CompositeEffect
    {
      Effects = [modifyPT, gainAbility],
    };
  }
}
