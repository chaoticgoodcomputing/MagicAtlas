namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile target card from a graveyard." — single-card graveyard-exile as an
/// activated-ability effect (Rule 701.7), the common graveyard-hoser pattern.
/// </summary>
[ActivatedEffectRule(Priority = 984)]
public sealed class ExileFromGraveyardEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    if (!Regex.IsMatch(
          trimmed,
          @"^Exile\s+target\s+card\s+from\s+a\s+graveyard$",
          RegexOptions.IgnoreCase))
    {
      return null;
    }
    return new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["card"],
          Zone = Zone.Graveyard,
        },
      },
    };
  }
}
