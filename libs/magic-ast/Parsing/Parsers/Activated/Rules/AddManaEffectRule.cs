namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;

/// <summary>
/// "Add {mana}" — e.g. "Add {G}", "Add {C}{C}{C}", "Add {W}{U}{B}{R}{G}". Also
/// handles "Add one mana of any color" (Crystal Grotto / Chromatic Lantern shape)
/// where the produced mana is a single choice across all five colors.
/// </summary>
[ActivatedEffectRule(Priority = 1000)]
public sealed class AddManaEffectRule : IActivatedEffectRule
{
  // SCALING / SEQUENTIAL / RESTRICTED mana clauses this rule does NOT model.
  // The Mana scalar is a mana EXPRESSION (symbols + "or"/commas), not prose; left
  // unguarded the trailing clause was silently swallowed into Mana with no unparsed
  // node and no residual — false coverage that inflates triage directYield. Match →
  // return null so the line falls to UnparsedEffect and surfaces as an honest,
  // pickable family (scaling-mana "for each X" / restricted-mana "Spend only to Y").
  private static readonly Regex UnmodeledManaClause = new(
    @"\bfor\s+each\b|\bfor\s+every\b|,?\s*then\s+add\b|\bspend\s+this\s+mana\s+only\b|\buntil\s+end\s+of\s+turn\b",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    // Normalize whitespace
    effectText = effectText.Trim();

    if (!effectText.StartsWith("Add ", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    // Extract the mana portion (everything after "Add" and before optional ".")
    var manaText = effectText[4..].Trim();
    if (manaText.EndsWith('.'))
    {
      manaText = manaText[..^1].Trim();
    }

    // "one mana of any color" optionally followed by a spend restriction —
    // "Spend this mana only to <X>." (Unclaimed Territory shape). The activated
    // ParseEffects flow runs a multi-sentence pre-pass first; when the second
    // sentence has no rule of its own it falls back to matching the WHOLE
    // combined string against each rule, so this rule must accept the joined
    // "Add one mana of any color. Spend this mana only to <X>." text. MAST
    // describes rather than executes, so the restriction is captured verbatim.
    var anyColorMatch = Regex.Match(
      manaText,
      @"^one\s+mana\s+of\s+any\s+color(?:\.\s+Spend\s+this\s+mana\s+only\s+to\s+(?<restriction>.+?))?$",
      RegexOptions.IgnoreCase
    );
    if (anyColorMatch.Success)
    {
      var restrictionGroup = anyColorMatch.Groups["restriction"];
      return new AddManaEffect
      {
        Mana = string.Empty,
        AnyColor = true,
        SpendRestriction = restrictionGroup.Success
          ? restrictionGroup.Value.Trim()
          : null,
      };
    }

    // The mana text should be a sequence of mana symbols like "{G}" or "{C}{C}{C}",
    // optionally a colour CHOICE ("{R} or {G}", "{W}, {B}, or {G}").
    if (string.IsNullOrWhiteSpace(manaText) || !manaText.Contains('{'))
    {
      return null;
    }

    // Bail on scaling / sequential / restricted mana (see UnmodeledManaClause).
    if (UnmodeledManaClause.IsMatch(manaText))
    {
      return null;
    }

    return new AddManaEffect { Mana = manaText, AnyColor = false };
  }
}
