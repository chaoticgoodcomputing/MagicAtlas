namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice a creature" — triggered player-choice sacrifice. The controller
/// picks any creature they control at resolution; there is no formal targeting
/// declaration (Rule 115.1 — only the "target" keyword creates a target), so
/// the reference kind is <see cref="ObjectReferenceKind.Any"/>. Covers the
/// classic upkeep-tax pattern used on cards like Necrite (FEM/ME2).
///
/// Distinct from <see cref="SacrificeTriggeredRule"/>, which handles the
/// pronoun forms "sacrifice it / this creature / this permanent" where the
/// object to sacrifice is already determined by the trigger context.
/// </summary>
[TriggeredRule]
public sealed class SacrificeAnyCreatureTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+a\s+creature\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
    {
      return false;
    }

    effect = new SacrificeEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Any,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
    };
    return true;
  }
}
