namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile target creature." — moves a targeted creature from whatever zone it
/// occupies to the exile zone.
///
/// CR 701.13a: "To exile an object, move it to the exile zone from wherever it is."
/// </summary>
[ActivatedEffectRule(Priority = 983)]
public sealed class ExileTargetCreatureEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Exile\s+target\s+creature\s*\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    if (!_pattern.IsMatch(effectText.Trim()))
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
          CardTypes = ["creature"],
        },
      },
    };
  }
}
