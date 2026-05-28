namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

[StaticRule(Priority = 968)]
public sealed class AsLongAsStaticGrantRule : IStaticRule
{
  // Leading form: "As long as <cond>, it/this creature [gets|has] <effect>."
  // <cond> is everything between "As long as " and the comma; <effect> is the
  // subject-led clause after the comma. Named "leading" because the condition
  // clause leads the sentence — contrast with the suffix pattern below.
  // Accepts both the pronoun "it" and the self-referential "this creature" /
  // "this permanent" subjects, because oracle text uses both forms (e.g. Leonin
  // Den-Guard uses "it gets" while Threshold-ability-word cards use "this
  // creature gets").
  private static readonly Regex _asLongAsLeadingPattern = new(
    @"^\s*As\s+long\s+as\s+(?<cond>[^,]+),\s*(?<effect>(?:it|this\s+(?:creature|permanent))\s+.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches the effect sub-clause of the leading form for P/T modifiers.
  // "it gets +0/+2", "it gets +1/+1", "this creature gets +2/+2", etc.
  // Both sides require an explicit '+' sign (oracle uses signed notation even
  // for zero modifiers).
  private static readonly Regex _itGetsPTPattern = new(
    @"^\s*(?:it|this\s+(?:creature|permanent))\s+gets\s+\+(?<p>\d+)/\+(?<t>\d+)\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches the compound leading-form effect "it/this creature gets +N/+M and
  // can't block" (Rule 509.1c blocker-side restriction bundled with a P/T buff).
  // Must be tried before _itGetsPTPattern to avoid the simple PT match consuming
  // only part of the compound clause.
  private static readonly Regex _itGetsPTAndCantBlockPattern = new(
    @"^\s*(?:it|this\s+(?:creature|permanent))\s+gets\s+\+(?<p>\d+)/\+(?<t>\d+)\s+and\s+can'?t\s+block\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches the effect sub-clause of the leading form for keyword grants.
  // "it has haste", "it has first strike", "this creature has first strike", etc.
  private static readonly Regex _itHasKeywordPattern = new(
    @"^\s*(?:it|this\s+(?:creature|permanent))\s+has\s+(?<kw>[A-Za-z][A-Za-z\s]*?)\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Soulbond leading form: "As long as <cond>, both creatures have <keyword>."
  // Handles the Soulbond paired-grant shape (Rule 702.95) where the effect clause
  // targets both paired creatures rather than the single self-referential "it".
  // Separate from _asLongAsLeadingPattern because that pattern requires the effect
  // to start with "it "; this pattern captures "both creatures have <keyword>" directly.
  private static readonly Regex _asLongAsSoulbondLeadingPattern = new(
    @"^\s*As\s+long\s+as\s+(?<cond>[^,]+),\s*both\s+creatures\s+have\s+(?<kw>[A-Za-z][A-Za-z\s]*?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Strips " as long as <cond>." from the end. The "main" group is everything
  // before the suffix; "cond" is the condition body without the trailing period.
  private static readonly Regex _asLongAsSuffixPattern = new(
    @"^\s*(?<main>.+?)\s+as\s+long\s+as\s+(?<cond>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches the sub-clause after suffix stripping for P/T modifiers.
  // Handles "+0/+3", "+1/+0", etc. (non-negative only — oracle uses explicit
  // +/- signs; negative modifiers use the dash form which we don't see here).
  private static readonly Regex _selfGetsPTPattern = new(
    @"^\s*This\s+(?:creature|permanent)\s+gets\s+\+(?<p>\d+)/\+(?<t>\d+)\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches the sub-clause for an unquoted keyword grant on any subject.
  // Subject (anything before "has") collapses to Self.
  private static readonly Regex _subjectHasKeywordPattern = new(
    @"^\s*\S.*?\s+has\s+(?<kw>[A-Za-z][A-Za-z\s]*?)\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Soulbond leading form: "As long as <name> is paired with another creature, both creatures have <keyword>."
    // Must be tried before the generic leading form because this effect clause starts with
    // "both creatures" rather than "it", so the generic leading pattern would not match it.
    var soulbondLeadingMatch = _asLongAsSoulbondLeadingPattern.Match(clause.RawText);
    if (soulbondLeadingMatch.Success)
    {
      var soulbondCond = soulbondLeadingMatch.Groups["cond"].Value.Trim();
      var soulbondKw = soulbondLeadingMatch.Groups["kw"].Value.Trim();
      var soulbondDuration = new AsLongAsDuration { Condition = soulbondCond };
      var soulbondGrantedAbility = StaticRuleHelpers.MapKeywordToStaticAbility(soulbondKw);
      if (soulbondGrantedAbility != null)
      {
        return
        [
          new StaticAbility
          {
            Effects = [new MagicAST.AST.Effects.Modification.GainAbilityEffect
            {
              Target = new ObjectReference { Kind = ObjectReferenceKind.BothPaired },
              GainedAbility = soulbondGrantedAbility,
              Duration = soulbondDuration,
            }],
          },
        ];
      }
    }

    // Leading form: "As long as <condition>, it/this creature [gets +N/+M | has <keyword>]."
    // Tried first because the suffix pattern's non-greedy <main> group would mis-parse
    // leading-form text (consuming only "As" as the subject before "long as").
    //
    // When an ability word is present (e.g. "Threshold — As long as …") the em-dash
    // prefix must be stripped before the leading pattern is applied, because the
    // pattern anchors on "^\s*As\s+long\s+as". The classifier has already captured
    // the word in classification.AbilityWord; we peel the prefix here the same way
    // the suffix branch does at line 3152.
    string? abilityWordForLeading = classification.AbilityWord;
    string leadingBodyText = clause.RawText;
    if (abilityWordForLeading is not null)
    {
      var emDashLeadingIdx = leadingBodyText.IndexOf('—');
      if (emDashLeadingIdx >= 0)
      {
        leadingBodyText = leadingBodyText[(emDashLeadingIdx + 1)..].TrimStart();
      }
    }

    var leadingMatch = _asLongAsLeadingPattern.Match(leadingBodyText);
    if (leadingMatch.Success)
    {
      var leadingCond = leadingMatch.Groups["cond"].Value.Trim();
      var effectText = leadingMatch.Groups["effect"].Value.Trim();
      var leadingDuration = new AsLongAsDuration { Condition = leadingCond };

      // Leading sub-parser C: "it/this creature gets +N/+M and can't block" (compound).
      // Tried before sub-parser A so the "and can't block" suffix doesn't break the
      // simple PT match on an unexpected fallthrough.
      var leadingPtCantBlockMatch = _itGetsPTAndCantBlockPattern.Match(effectText);
      if (leadingPtCantBlockMatch.Success)
      {
        var power = int.Parse(leadingPtCantBlockMatch.Groups["p"].Value);
        var toughness = int.Parse(leadingPtCantBlockMatch.Groups["t"].Value);
        return
        [
          new StaticAbility
          {
            AbilityWord = abilityWordForLeading,
            Effects =
            [
              new MagicAST.AST.Effects.Modification.ModifyPTEffect
              {
                Target = ObjectReference.Self(),
                PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
                ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
                Duration = leadingDuration,
              },
              new MagicAST.AST.Effects.Combat.CantBlockEffect
              {
                Duration = leadingDuration,
              },
            ],
          },
        ];
      }

      // Leading sub-parser A: "it/this creature gets +N/+M"
      var leadingPtMatch = _itGetsPTPattern.Match(effectText);
      if (leadingPtMatch.Success)
      {
        var power = int.Parse(leadingPtMatch.Groups["p"].Value);
        var toughness = int.Parse(leadingPtMatch.Groups["t"].Value);
        return
        [
          new StaticAbility
          {
            AbilityWord = abilityWordForLeading,
            Effects = [new MagicAST.AST.Effects.Modification.ModifyPTEffect
            {
              Target = ObjectReference.Self(),
              PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
              ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
              Duration = leadingDuration,
            }],
          },
        ];
      }

      // Leading sub-parser B: "it/this creature has <keyword>"
      var leadingKwMatch = _itHasKeywordPattern.Match(effectText);
      if (leadingKwMatch.Success)
      {
        var kw = leadingKwMatch.Groups["kw"].Value.Trim();
        var grantedAbility = StaticRuleHelpers.MapKeywordToStaticAbility(kw);
        if (grantedAbility != null)
        {
          return
          [
            new StaticAbility
            {
              AbilityWord = abilityWordForLeading,
              Effects = [new MagicAST.AST.Effects.Modification.GainAbilityEffect
              {
                Target = ObjectReference.Self(),
                GainedAbility = grantedAbility,
                Duration = leadingDuration,
              }],
            },
          ];
        }
      }

      // Recognise the leading form but can't parse the effect — fall through to suffix.
      // Do NOT return null here; the suffix branch below may still succeed if the text
      // happens to also satisfy the suffix pattern (unlikely but safe).
    }

    // Peel " as long as <condition>." from the end of the clause.
    var suffixMatch = _asLongAsSuffixPattern.Match(clause.RawText);
    if (!suffixMatch.Success)
    {
      return null;
    }

    var remainingText = suffixMatch.Groups["main"].Value.Trim();
    var conditionText = suffixMatch.Groups["cond"].Value.Trim();
    var duration = new AsLongAsDuration { Condition = conditionText };

    // Strip any ability-word prefix ("Threshold — ", "Metalcraft — ", etc.)
    // from remainingText. The classifier has already captured the word into
    // classification.AbilityWord (Rule 207.2c). Without stripping, the em-dash
    // prefix breaks every downstream sub-parser regex that anchors on "This".
    string? abilityWord = classification.AbilityWord;
    if (abilityWord is not null)
    {
      var emDashIdx = remainingText.IndexOf('—');
      if (emDashIdx >= 0)
      {
        remainingText = remainingText[(emDashIdx + 1)..].TrimStart();
      }
    }

    // Sub-parser A: "This creature/This permanent gets +N/+M"
    var ptMatch = _selfGetsPTPattern.Match(remainingText);
    if (ptMatch.Success)
    {
      var power = int.Parse(ptMatch.Groups["p"].Value);
      var toughness = int.Parse(ptMatch.Groups["t"].Value);
      return
      [
        new StaticAbility
        {
          AbilityWord = abilityWord,
          Effects = [new MagicAST.AST.Effects.Modification.ModifyPTEffect
          {
            Target = ObjectReference.Self(),
            PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
            ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
            Duration = duration,
          }],
        },
      ];
    }

    // Sub-parser B: "[subject] has [keyword]" — unquoted keyword grant.
    // Subject collapses to Self (card-name-as-subject oracle convention).
    var kwMatch = _subjectHasKeywordPattern.Match(remainingText);
    if (kwMatch.Success)
    {
      var kw = kwMatch.Groups["kw"].Value.Trim();
      var grantedAbility = StaticRuleHelpers.MapKeywordToStaticAbility(kw);
      if (grantedAbility != null)
      {
        return
        [
          new StaticAbility
          {
            AbilityWord = abilityWord,
            Effects = [new MagicAST.AST.Effects.Modification.GainAbilityEffect
            {
              Target = ObjectReference.Self(),
              GainedAbility = grantedAbility,
              Duration = duration,
            }],
          },
        ];
      }
    }

    return null;
  }
}
