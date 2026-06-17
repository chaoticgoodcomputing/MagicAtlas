namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice this [enchantment|artifact|land|sorcery|instant|planeswalker|…]" —
/// a self-sacrifice effect where the oracle text names the source's own card type
/// (e.g. "sacrifice this enchantment") rather than the generic "sacrifice this
/// permanent" form. Maps to a <see cref="SacrificeEffect"/> with
/// <see cref="ObjectReferenceKind.Self"/>.
///
/// <para>
/// Rule 701.21a: "To sacrifice a permanent, its controller moves it from the
/// battlefield directly to its owner's graveyard." MAST records the target as
/// Self (the source of the ability); the engine performs the move.
/// </para>
///
/// <para>
/// This rule supersedes any broader "sacrifice this permanent" fallback by
/// Priority = 65 (above the default 50 but below the 70 used by the
/// <c>SacrificeAtEndStepTriggeredRule</c> which handles the delayed form).
/// Examples: Underworld Breach (THB): "sacrifice this enchantment."
/// </para>
///
/// <para>
/// Rule citations: 701.21a (Sacrifice), 201.4 (self-name reference).
/// </para>
/// </summary>
[TriggeredRule(Priority = 65)]
public sealed class SacrificeSelfTriggeredRule : ITriggeredRule
{
  // Matches "sacrifice this <type_word>." where <type_word> is any single word
  // (enchantment, artifact, land, sorcery, instant, planeswalker, equipment, etc.).
  // Distinct from SacrificeTriggeredRule which handles "this creature" / "this permanent"
  // and from SacrificeAtEndStepTriggeredRule which handles the delayed-trigger form.
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+this\s+(?<type>\w+)\.?$",
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

    // Exclude the already-handled shapes "creature" and "permanent" so
    // SacrificeTriggeredRule remains the canonical handler for those forms
    // (it maps to ObjectReference.It(), which is accurate for pronouns that
    // reference the trigger subject). This rule is for type-named self-sacrifice
    // ("this enchantment", "this artifact", "this land", etc.) where the target
    // is unambiguously the source permanent → ObjectReference.Self().
    var typeWord = m.Groups["type"].Value.ToLowerInvariant();
    if (typeWord is "creature" or "permanent")
    {
      return false;
    }

    effect = new SacrificeEffect { Target = ObjectReference.Self() };
    return true;
  }
}
