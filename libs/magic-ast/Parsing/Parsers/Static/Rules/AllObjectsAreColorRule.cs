namespace MagicAST.Parsing.Parsers.Static;

using System.Linq;
using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Parses the "All &lt;subject&gt; are &lt;color(s)&gt;." oracle template
/// (Darkest Hour: "All creatures are black.") — a static layer-5 continuous
/// effect that sets the color of every object matching a broad, unqualified
/// subject filter.
///
/// <para>
/// CR 105.1 (verbatim): "There are five colors in the Magic game: white, blue,
/// black, red, and green."
/// </para>
///
/// <para>
/// <b>CR 611.1:</b> "A continuous effect modifies characteristics of objects,
/// modifies control of objects, or affects players or the rules of the game,
/// for a fixed or indefinite period."
/// </para>
///
/// <para>
/// Reuses <see cref="ChangeColorEffect"/> (the same node
/// <see cref="MagicAST.Parsing.Parsers.Activated.Rules.ChangeColorEffectRule"/>
/// emits for the singular-target "Target creature becomes [color]"
/// activated-ability shape), scoped here to a broad <c>Each</c>
/// <see cref="ObjectReference"/> instead of a singular <c>Target</c>. No
/// <c>Duration</c> is set: the color-setting is an ongoing static ability that
/// lasts as long as this permanent remains on the battlefield, mirroring
/// <see cref="AllObjectsAreColorlessRule"/>'s undurationed sibling effects.
/// </para>
///
/// <para>
/// The subject noun is resolved through a small whitelist of broad, unqualified
/// card-type groups (creatures, permanents, lands, artifacts, enchantments,
/// planeswalkers) — an unrecognized subject or a controller-qualified/multi-noun
/// subject (e.g. "creatures you control", "creatures and planeswalkers")
/// declines the match rather than guessing, falling through to a more specific
/// sibling rule or the fallback parser. The color alternation is restricted to
/// the five named colors (CR 105.1); "colorless" is owned by the sibling
/// <see cref="AllObjectsAreColorlessRule"/> and is not matched here.
/// </para>
///
/// <para>
/// Priority 967 — grouped with the sibling "All &lt;subject&gt; are/have
/// &lt;X&gt;" static rules (<see cref="AllObjectsAddTypeRule"/> at 969,
/// <see cref="AllObjectsAreColorlessRule"/> at 968); this rule's color-name
/// alternation is disjoint from both the type-token alternation and
/// "colorless", so relative dispatch ordering among the three is immaterial.
/// </para>
/// </summary>
[StaticRule(Priority = 967)]
public sealed class AllObjectsAreColorRule : IStaticRule
{
  // "All <subject> are <color>[, <color>]*[,]? [and <color>]." — CR 105.1's five
  // named colors only; "colorless" is handled by AllObjectsAreColorlessRule.
  private static readonly Regex _pattern = new(
    @"^\s*All\s+(?<subject>[A-Za-z]+)\s+are\s+"
    + @"(?<colors>(?:white|blue|black|red|green)"
    + @"(?:\s*,\s*(?:white|blue|black|red|green))*"
    + @"(?:\s*,?\s+and\s+(?:white|blue|black|red|green))?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _colorCodes =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };

  // Whitelist of broad, unqualified subject nouns -> singular card-type token.
  private static readonly IReadOnlyDictionary<string, string> _subjectCardTypes =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["creatures"] = "creature",
      ["permanents"] = "permanent",
      ["lands"] = "land",
      ["artifacts"] = "artifact",
      ["enchantments"] = "enchantment",
      ["planeswalkers"] = "planeswalker",
    };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var m = _pattern.Match(clause.RawText);
    if (!m.Success)
    {
      return null;
    }

    var subject = m.Groups["subject"].Value.Trim();
    if (!_subjectCardTypes.TryGetValue(subject, out var cardType))
    {
      return null;
    }

    var colorsRaw = m.Groups["colors"].Value;
    var colorTokens = Regex
      .Split(colorsRaw, @"\s*,\s*|\s+and\s+", RegexOptions.IgnoreCase)
      .Where(t => !string.IsNullOrWhiteSpace(t))
      .ToList();

    var colors = new List<string>();
    foreach (var token in colorTokens)
    {
      if (!_colorCodes.TryGetValue(token.Trim(), out var code))
      {
        return null;
      }

      colors.Add(code);
    }

    if (colors.Count == 0)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new ChangeColorEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = [cardType],
              },
            },
            Colors = colors,
          },
        ],
      },
    ];
  }
}
