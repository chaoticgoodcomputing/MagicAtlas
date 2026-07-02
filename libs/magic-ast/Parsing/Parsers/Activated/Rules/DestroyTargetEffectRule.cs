namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target &lt;type&gt;." — single-target destroy as an activated-ability effect.
/// Handles bare card-type targets: creature, artifact, enchantment, land, planeswalker,
/// permanent; and two-type disjunctions: "artifact or enchantment",
/// "creature or planeswalker", etc.
///
/// CR 701.8a: "To destroy a permanent, move it from the battlefield to its owner's
/// graveyard."
///
/// GUARD: handles only "Destroy target &lt;type&gt;" shapes. Does NOT handle Exile
/// (sibling family F14 owns "Exile target creature").
/// </summary>
[ActivatedEffectRule(Priority = 600)]
public sealed class DestroyTargetEffectRule : IActivatedEffectRule
{
  private static readonly string _typeGroup =
    "creature|artifact|enchantment|land|planeswalker|permanent";

  // Single-type: "Destroy target artifact."
  private static readonly Regex SinglePattern = new(
    $@"^Destroy\s+target\s+(?<type>{_typeGroup})\s*\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Two-type disjunction: "Destroy target artifact or enchantment."
  private static readonly Regex DisjunctionPattern = new(
    $@"^Destroy\s+target\s+(?<type1>{_typeGroup})\s+or\s+(?<type2>{_typeGroup})\s*\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim();

    // Try disjunction first (more specific)
    var disjMatch = DisjunctionPattern.Match(text);
    if (disjMatch.Success)
    {
      var type1 = disjMatch.Groups["type1"].Value.ToLowerInvariant();
      var type2 = disjMatch.Groups["type2"].Value.ToLowerInvariant();
      return new DestroyEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = [type1, type2] },
        },
      };
    }

    // Single-type fallback
    var singleMatch = SinglePattern.Match(text);
    if (!singleMatch.Success)
    {
      return null;
    }

    var cardType = singleMatch.Groups["type"].Value.ToLowerInvariant();
    return new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = [cardType] },
      },
    };
  }
}
