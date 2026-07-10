namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice a land" — triggered player-choice sacrifice of an unqualified land.
/// The controller picks any land they control at resolution; there is no formal
/// targeting declaration (Rule 115.1 — only the "target" keyword creates a target),
/// so the reference kind is <see cref="ObjectReferenceKind.Any"/>. Mirrors
/// <see cref="SacrificeAnyCreatureTriggeredRule"/> ("sacrifice a creature"), swapping
/// the filtered card type from creature to land.
///
/// <para>
/// Example: Serendib Djinn (ARN): "At the beginning of your upkeep, sacrifice a
/// land."
/// </para>
///
/// <para>
/// Distinct from <see cref="SacrificeLandAndDiscardHandRule"/> ("sacrifice a land
/// and discard your hand"), which is a longer compound sentence; this rule is
/// anchored to the bare "sacrifice a land" clause only.
/// </para>
///
/// CR 701.21a (verbatim): "To sacrifice a permanent, its controller moves it from
/// the battlefield directly to its owner's graveyard."
/// </summary>
[TriggeredRule]
public sealed class SacrificeALandTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+a\s+land\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new SacrificeEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Any,
        Filter = new ObjectFilter { CardTypes = ["land"] },
      },
    };
    return true;
  }
}
