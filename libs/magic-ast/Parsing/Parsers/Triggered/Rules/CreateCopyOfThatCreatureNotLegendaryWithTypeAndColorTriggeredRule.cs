namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "create a token that's a copy of that creature, except it's not legendary and
/// it's a [P]/[T] [color] [Subtype] in addition to its other colors and types."
///
/// The Ratadrabik of Urborg pattern: a copy of the dying creature with three
/// structured "except"-clauses applied in order:
/// <list type="number">
/// <item><see cref="SupertypeRemover"/> strips Legendary (CR 704.5j — legend rule
///   would otherwise cull the token copy if the original creature is still on the battlefield).</item>
/// <item><see cref="PowerToughnessOverride"/> fixes the token at 2/2 regardless of
///   the original's power/toughness (CR 707.2 copiable values).</item>
/// <item><see cref="ColorAdder"/> adds black "in addition to its other colors"
///   (CR 105.3 additive colour change — the token keeps the original's colours).</item>
/// <item><see cref="TypeAdder"/> adds Zombie "in addition to its other types"
///   (CR 205.1b — subtype addition preserves all existing subtypes).</item>
/// </list>
///
/// <para>
/// "that creature" back-references the object named by the trigger's filter (the
/// dying legendary creature), encoded as <see cref="ObjectReferenceKind.ThatCreature"/>.
/// </para>
///
/// <para>
/// Rule citations: CR 707.2 (copy semantics), CR 105.3 (additive colour change),
/// CR 205.1b (subtype addition), CR 704.5j (legend rule), CR 111.1 (token creation).
/// </para>
///
/// <para>
/// ANCHORED (^…$): the full surface phrase is anchored to prevent substring matches
/// against any sibling triggered rule. Priority 76 — above the general copy-of-this-creature
/// rules (70–75), well above the generic token rule (50).
/// </para>
/// </summary>
[TriggeredRule(Priority = 76)]
public sealed class CreateCopyOfThatCreatureNotLegendaryWithTypeAndColorTriggeredRule : ITriggeredRule
{
  // "create a token that's a copy of that creature, except it's not legendary and
  //  it's a <power>/<toughness> <color> <Subtype> in addition to its other colors and types"
  // Terminal period is stripped by the dispatcher before TryMatch is called.
  private static readonly Regex _pattern = new(
    @"^create\s+a\s+token\s+that(?:'s|'s)\s+a\s+copy\s+of\s+that\s+creature,\s+except\s+it(?:'s|'s)\s+not\s+legendary\s+and\s+it(?:'s|'s)\s+a\s+(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtype>[A-Z][A-Za-z]+)\s+in\s+addition\s+to\s+its\s+other\s+colors\s+and\s+types$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Color-name → single-letter code map (WUBRG).
  private static readonly IReadOnlyDictionary<string, string> _colorCode =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var power = int.Parse(m.Groups["power"].Value);
    var toughness = int.Parse(m.Groups["toughness"].Value);
    var colorName = m.Groups["color"].Value;
    var subtype = m.Groups["subtype"].Value;
    // Normalise subtype to capitalised form (oracle text capitalises creature subtypes).
    var subtypeNorm = char.ToUpperInvariant(subtype[0]) + subtype[1..];

    if (!_colorCode.TryGetValue(colorName, out var colorCode))
    {
      return false;
    }

    effect = new CopyEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.ThatCreature },
      Modifications =
      [
        new SupertypeRemover { Supertypes = ["Legendary"] },
        new PowerToughnessOverride
        {
          Power = LiteralQuantity.Of(power),
          Toughness = LiteralQuantity.Of(toughness),
        },
        new ColorAdder { Colors = [colorCode] },
        new TypeAdder { Subtypes = [subtypeNorm] },
      ],
    };
    return true;
  }
}
