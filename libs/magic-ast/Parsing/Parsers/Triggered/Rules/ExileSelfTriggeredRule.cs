namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "exile it" / "exile this creature" / "exile this permanent" — a self-exile
/// effect where the triggered ability's source exiles itself (or the trigger
/// subject). Maps to an <see cref="ExileEffect"/> with
/// <see cref="ObjectReferenceKind.It"/> (the trigger subject / source).
///
/// <para>
/// Rule 701.13a (verbatim): "To exile an object, move it to the exile zone from
/// wherever it is." MAST records the target as It (the subject of the trigger,
/// i.e., the source permanent); the engine performs the zone change.
/// </para>
///
/// <para>
/// Examples: Eternal Scourge (EMN): "exile this creature."
/// </para>
///
/// <para>
/// Rule citations: 701.13a (Exile action), 109.2 (object reference), 603.1-603.2
/// (triggered ability machinery).
/// </para>
/// </summary>
[TriggeredRule(Priority = 65)]
public sealed class ExileSelfTriggeredRule : ITriggeredRule
{
  // Matches "exile it", "exile this creature", "exile this permanent" (with optional trailing period).
  // Anchored ^…$ to avoid matching "exile this creature until it leaves the battlefield"
  // (ExileUntilLeavesTriggeredRule) or other longer forms as a substring.
  private static readonly Regex _pattern = new(
    @"^exile\s+(it|this\s+creature|this\s+permanent)\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();
    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new ExileEffect { Target = ObjectReference.It() };
    return true;
  }
}
