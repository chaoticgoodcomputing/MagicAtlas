namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target &lt;type&gt;." — single-target destroy as an activated-ability effect.
/// Handles bare card-type targets: creature, artifact, enchantment, land, planeswalker,
/// permanent.
///
/// CR 701.8a: "To destroy a permanent, move it from the battlefield to its owner's
/// graveyard."
///
/// GUARD: handles only "Destroy target &lt;single-type&gt;". Does NOT handle Exile
/// (sibling family F14 owns "Exile target creature").
/// </summary>
[ActivatedEffectRule(Priority = 600)]
public sealed class DestroyTargetEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Destroy\s+target\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent)\s*\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim();
    var match = Pattern.Match(text);
    if (!match.Success)
    {
      return null;
    }

    var cardType = match.Groups["type"].Value.ToLowerInvariant();

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
