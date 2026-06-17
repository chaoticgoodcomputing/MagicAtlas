namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "it deals X damage to any target, where X is the number of [Subtype]s you
/// control." — the entering permanent deals damage equal to a count of a subtype
/// you control to any legal target. Covers Dragon Tempest's second ability:
/// "it deals X damage to any target, where X is the number of Dragons you control."
///
/// Rule 603: triggered abilities resolve by executing their effects. Rule 120.1–
/// 120.2: a source deals damage to a permanent or player. Rule 115.4: "any
/// target" may be any player, creature, planeswalker, or battle.
///
/// "It" is the anaphoric pronoun (CR 113.8b) referring to the entering Dragon —
/// the permanent the trigger's filter matched. Modelled as
/// <see cref="ObjectReferenceKind.It"/>. The amount "X" is a
/// <see cref="CountQuantity"/> — the count of permanents matching the filter
/// (subtype + controller = You). "Where X is the number of…" is a definitional
/// clause naming the quantity; the variable letter X has no independent identity
/// here — the structured <see cref="CountQuantity"/> replaces it.
///
/// Distinct from <see cref="SelfDealsDamageToAnyTargetTriggeredRule"/>, which
/// handles a fixed literal amount ("it deals 3 damage").
/// </summary>
[TriggeredRule(Priority = 65)]
public sealed class ItDealsCountDamageToAnyTargetRule : ITriggeredRule
{
  // Matches: "it deals X damage to any target, where X is the number of <Subtype>s you control"
  // The subtype word must begin with an uppercase letter (oracle subtype convention, Rule 205.3m).
  // The group captures the singular root; the trailing plural 's' is consumed by the outer pattern.
  // Use a lazy quantifier on the inner [A-Za-z]+ so the outer 's' can claim the trailing plural letter.
  private static readonly Regex _pattern = new(
    @"^it\s+deals?\s+X\s+damage\s+to\s+any\s+target,?\s+where\s+X\s+is\s+the\s+number\s+of\s+(?<subtype>[A-Z][A-Za-z]+?)s?\s+you\s+control\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var rawSubtype = m.Groups["subtype"].Value;
    // Normalise to capitalised form (oracle text capitalises subtypes — Rule 205.3m).
    // The captured group is the singular form (lazy quantifier stops before the trailing 's').
    var subtype = char.ToUpperInvariant(rawSubtype[0]) + rawSubtype[1..];

    effect = new DealDamageEffect
    {
      Source = ObjectReference.It(),
      Amount = new CountQuantity
      {
        CountOf = new ObjectFilter
        {
          Subtypes = [subtype],
          Controller = ControllerFilter.You,
        },
      },
      Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
    };
    return true;
  }
}
