namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "you may exile an instant card with mana value 2 or less from your hand" — the
/// Imprint triggered-ability effect (Isochron Scepter, Mirrodin / MRD:200).
///
/// <para>
/// CR 702.38 (Imprint): an imprint ability exiles a card from the controller's hand
/// onto this permanent so a second ability may reference it. The exile is a one-shot
/// effect (Rule 701.13) from Zone.Hand: the controller chooses an eligible card from
/// their hand (not a targeted card — the word "target" does not appear) and exiles it.
/// The choice is modelled as <see cref="ObjectReferenceKind.Any"/> (controller-choice
/// reference, CR 109.5) with Zone.Hand so the interaction layer can distinguish
/// "from hand" from "on the battlefield". The "you may" gate is modelled as an
/// <see cref="OptionalEffect"/> per ADR 0005.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class ExileInstantFromHandImprintTriggeredRule : ITriggeredRule
{
  // "you may exile an instant card with mana value 2 or less from your hand"
  private static readonly Regex Pattern = new(
    @"^you\s+may\s+exile\s+an?\s+instant\s+card\s+with\s+mana\s+value\s+(?<mv>\d+)\s+or\s+less\s+from\s+your\s+hand$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    if (!int.TryParse(m.Groups["mv"].Value, out var mv))
    {
      return false;
    }

    effect = new OptionalEffect
    {
      Inner = new ExileEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Any,
          Filter = new ObjectFilter
          {
            CardTypes = ["instant"],
            ManaValueComparison = new Comparison
            {
              Operator = ComparisonOperator.LessThanOrEqual,
              Value = mv,
            },
            Controller = ControllerFilter.You,
            Zone = Zone.Hand,
          },
        },
      },
    };
    return true;
  }
}
