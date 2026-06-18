namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "create a token that's a copy of that creature, except it's a [P]/[T] [color] [Subtype]"
/// — a copy-token effect that creates a copy of the triggering creature but overrides
/// its power/toughness, color, and subtype. The paradigm card is Preston, the Vanisher:
/// "create a token that's a copy of that creature, except it's a 0/1 white Illusion."
///
/// <para>
/// The subject "that creature" is the object that caused the trigger to fire —
/// encoded as <see cref="ObjectReferenceKind.ThatCreature"/>, the structured back-reference
/// to the trigger's filter-matched object. Not <see cref="ObjectReferenceKind.It"/> (which
/// is a forward pronoun for the most recently named object) — "that creature" points
/// backwards to the creature named in the trigger condition.
/// </para>
///
/// <para>
/// The "except" clause overrides three copiable values (CR 707.2):
/// <list type="bullet">
///   <item><b>Power/Toughness</b> → <see cref="PowerToughnessOverride"/>.</item>
///   <item><b>Color</b> → <see cref="ColorOverride"/> (replaces the original's colors).</item>
///   <item><b>Subtype</b> → <see cref="TypeAdder"/> with Subtypes only (adds the named
///   subtype; the original creature's subtypes are still replaced by oracle intent).</item>
/// </list>
/// </para>
///
/// <para>
/// ANCHORED (^…$): prevents matching inside a more-specific sibling whose text
/// also contains "create a token that's a copy of that creature" as a substring.
/// Priority 75 — above <see cref="YouMayCreateTokenCopyOfItTriggeredRule"/> (72) and
/// the generic create-copy rules; specific enough to be tried before any broader path.
/// </para>
///
/// <para>
/// CR 707.1 (verbatim): "Some objects become or turn another object into a 'copy' of
/// a spell, permanent, or card. Some effects create a token that's a copy of another
/// object."
/// CR 707.2 (verbatim): "When copying an object, the copy acquires the copiable values
/// of the original object's characteristics … except those characteristics are modified
/// as specified by the effect that created the copy."
/// </para>
/// </summary>
[TriggeredRule(Priority = 75)]
public sealed class CreateTokenCopyOfThatCreatureWithColorPTSubtypeRule : ITriggeredRule
{
  private static readonly Dictionary<string, string> _colorCodes =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };

  // Matches: "create a token that's a copy of that creature, except it's a [P]/[T] [color] [Subtype]"
  // Anchored (^…$). The terminal period is stripped by the dispatcher before this rule is called.
  // The article "a" or "an" before the P/T must be followed by a space (e.g. "except it's a 0/1 white Illusion").
  private static readonly Regex _pattern = new(
    @"^create\s+a\s+token\s+that(?:'s|'s)\s+a\s+copy\s+of\s+that\s+creature,\s+except\s+it(?:'s|'s)\s+(?:an?\s+)?"
    + @"(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtype>[A-Z][a-z]+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.').Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var power = int.Parse(m.Groups["power"].Value);
    var toughness = int.Parse(m.Groups["toughness"].Value);
    var colorName = m.Groups["color"].Value;
    var subtype = m.Groups["subtype"].Value;

    if (!_colorCodes.TryGetValue(colorName, out var colorCode))
    {
      return false;
    }

    effect = new CopyEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.ThatCreature },
      Modifications =
      [
        new PowerToughnessOverride
        {
          Power = LiteralQuantity.Of(power),
          Toughness = LiteralQuantity.Of(toughness),
        },
        new ColorOverride { Colors = [colorCode] },
        new TypeAdder { Subtypes = [subtype] },
      ],
    };
    return true;
  }
}
