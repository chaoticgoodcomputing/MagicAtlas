namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Untap target [subtype]" — "Untap target Forest.", "Untap target creature.",
/// "Untap another target creature or land [you control]." First-cut single-token
/// target recognition (Rule 701.20).
/// </summary>
[ActivatedEffectRule(Priority = 993)]
public sealed class UntapEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim();
    if (text.EndsWith('.'))
    {
      text = text[..^1].Trim();
    }

    if (!text.StartsWith("Untap ", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    var remainder = text["Untap ".Length..].Trim();

    // "another target creature or land [you control]" — excludes the source
    // permanent ("another") and accepts creature or land (Rule 701.20 / 115.1).
    if (remainder.StartsWith("another target ", StringComparison.OrdinalIgnoreCase))
    {
      var afterAnotherTarget = remainder["another target ".Length..].Trim();
      var creatureOrLandMatch = Regex.Match(
        afterAnotherTarget,
        @"^(?:creature\s+or\s+land|land\s+or\s+creature)(?:\s+you\s+control)?$",
        RegexOptions.IgnoreCase
      );
      if (creatureOrLandMatch.Success)
      {
        var hasController = afterAnotherTarget.Contains(" you control", StringComparison.OrdinalIgnoreCase);
        return new UntapEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature", "land"],
              Characteristics = [Characteristic.Other("another")],
              Controller = hasController ? ControllerFilter.You : null,
            },
          },
        };
      }
    }

    if (!remainder.StartsWith("target ", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    var subtype = remainder["target ".Length..].Trim();
    if (string.IsNullOrEmpty(subtype) || subtype.Contains(' '))
    {
      // Multi-word filter (e.g., "target tapped creature") — beyond this cut.
      return null;
    }

    return new UntapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { Subtypes = [subtype] },
      },
    };
  }
}
