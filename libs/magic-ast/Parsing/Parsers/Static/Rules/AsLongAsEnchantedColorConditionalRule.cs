namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "As long as enchanted creature is [color], it gets +N/+N and has [keyword]."
/// "As long as enchanted creature is [color], it gets +N/+N and all creatures able to block it do so."
///
/// <para>
/// Produces a <see cref="StaticAbility"/> with the color condition on
/// <see cref="StaticAbility.Condition"/> (not encoded in a per-effect Duration)
/// and <c>EnchantedOrEquipped</c> as the target of each effect.
/// Both effects originate from one oracle clause gated by one condition (CR 611 —
/// a static ability creates one continuous effect layer).
/// The condition is stored at ability scope to avoid duplicating it across effects
/// and to separate "what applies" (the effects) from "when it applies" (the condition).
/// </para>
///
/// <para>
/// Two oracle-text shapes map here:
/// <list type="bullet">
///   <item>
///     <description>
///       PT-and-keyword grant: "it gets +1/+1 and has deathtouch" →
///       <see cref="ModifyPTEffect"/> + <see cref="GainAbilityEffect"/> (both targeting
///       <c>EnchantedOrEquipped</c>).
///     </description>
///   </item>
///   <item>
///     <description>
///       PT-and-lure: "it gets +1/+1 and all creatures able to block it do so" →
///       <see cref="ModifyPTEffect"/> + <see cref="AllMustBlockEffect"/> (Rule 509.1c;
///       every creature that can legally block the enchanted creature must do so).
///     </description>
///   </item>
/// </list>
/// </para>
///
/// <para>
/// Sits at Priority 975 — above <see cref="AsLongAsPTAndKeywordGrantRule"/> (970)
/// so the enchanted-creature shapes (which differ in target and condition placement)
/// are claimed before that rule's <c>ObjectReference.Self()</c> path.
/// </para>
/// </summary>
[StaticRule(Priority = 975)]
public sealed class AsLongAsEnchantedColorConditionalRule : IStaticRule
{
  // "As long as enchanted creature is <color>, it gets +N/+N and has <keyword>."
  // Trailing reminder text in parentheses is permitted (StripReminderText removes it).
  private static readonly Regex _ptAndKeywordPattern = new(
    @"^\s*As\s+long\s+as\s+(?<cond>enchanted\s+creature\s+is\s+\w+),\s*it\s+gets\s+\+(?<p>\d+)/\+(?<t>\d+)\s+and\s+has\s+(?<kw>[A-Za-z][A-Za-z\s]*?)\s*\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "As long as enchanted creature is <color>, it gets +N/+N and all creatures able to block it do so."
  private static readonly Regex _ptAndLurePattern = new(
    @"^\s*As\s+long\s+as\s+(?<cond>enchanted\s+creature\s+is\s+\w+),\s*it\s+gets\s+\+(?<p>\d+)/\+(?<t>\d+)\s+and\s+all\s+creatures\s+able\s+to\s+block\s+it\s+do\s+so\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var text = StaticRuleHelpers.StripReminderText(clause.RawText);

    // Shape 1: "As long as enchanted creature is [color], it gets +N/+N and has [keyword]."
    var kwMatch = _ptAndKeywordPattern.Match(text);
    if (kwMatch.Success)
    {
      var condText = kwMatch.Groups["cond"].Value.Trim();
      var power = int.Parse(kwMatch.Groups["p"].Value);
      var toughness = int.Parse(kwMatch.Groups["t"].Value);
      var keyword = kwMatch.Groups["kw"].Value.Trim();

      var grantedAbility = StaticRuleHelpers.MapKeywordToStaticAbility(keyword);
      if (grantedAbility is null)
      {
        // Keyword not yet supported — decline so an honest fallback handles it.
        return null;
      }

      var enchanted = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };
      return
      [
        new StaticAbility
        {
          Condition = new MagicAST.AST.Abilities.OtherCondition { Text = condText },
          Effects =
          [
            new ModifyPTEffect
            {
              Target = enchanted,
              PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
              ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
            },
            new GainAbilityEffect
            {
              Target = enchanted,
              GainedAbility = grantedAbility,
            },
          ],
        },
      ];
    }

    // Shape 2: "As long as enchanted creature is [color], it gets +N/+N and all creatures able to block it do so."
    var lureMatch = _ptAndLurePattern.Match(text);
    if (lureMatch.Success)
    {
      var condText = lureMatch.Groups["cond"].Value.Trim();
      var power = int.Parse(lureMatch.Groups["p"].Value);
      var toughness = int.Parse(lureMatch.Groups["t"].Value);

      var enchanted = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };
      return
      [
        new StaticAbility
        {
          Condition = new MagicAST.AST.Abilities.OtherCondition { Text = condText },
          Effects =
          [
            new ModifyPTEffect
            {
              Target = enchanted,
              PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
              ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
            },
            new AllMustBlockEffect
            {
              Target = enchanted,
            },
          ],
        },
      ];
    }

    return null;
  }
}
