namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "This [permanent] becomes a [P/T] [colors] [subtype] [card types] creature with
/// [keyword] until end of turn." — the Keyrune/Monument "animate" template. Emits a
/// single <see cref="BecomesCreatureEffect"/> describing the full set of
/// characteristics the source permanent takes on for the duration.
///
/// <para>
/// There is no keyword <em>action</em> for "becomes a creature"; this is one
/// continuous effect (CR 611) that sets card types/subtypes (CR 205), power/
/// toughness (CR 208), colors (CR 105), and grants a keyword ability (CR 113.6).
/// Layer/timestamp ordering (CR 613) is engine territory and is not modeled.
/// </para>
///
/// <para>
/// Examples:
/// <list type="bullet">
///   <item>Azorius Keyrune — "This artifact becomes a 2/2 white and blue Bird
///   artifact creature with flying until end of turn."</item>
///   <item>Gruul Keyrune — "This artifact becomes a 3/2 red and green Beast artifact
///   creature with trample until end of turn."</item>
/// </list>
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 985)]
public sealed class BecomesCreatureEffectRule : IActivatedEffectRule
{
  // "This artifact becomes a 2/2 white and blue Bird artifact creature with flying
  // until end of turn". The <spec> group is the run of words between the P/T box and
  // the literal "creature" head noun: colors ("white and blue"), the subtype ("Bird"),
  // and any non-creature card types ("artifact"). We classify those words afterward.
  private static readonly Regex _pattern = new(
    @"^This\s+\w+\s+becomes\s+a\s+(?<p>\d+|X)/(?<t>\d+|X)\s+(?<spec>.+?)\s+creature(?:\s+with\s+(?<kw>[a-z]+(?:\s+strike)?))?\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _colorCodes =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"]     = "W",
      ["blue"]      = "U",
      ["black"]     = "B",
      ["red"]       = "R",
      ["green"]     = "G",
      ["colorless"] = "C",
    };

  // Non-creature card types that can appear in an animate spec (CR 205.2a). "creature"
  // is the matched head noun and is appended separately; the source's own type
  // (artifact) is restated in the spec.
  private static readonly IReadOnlySet<string> _cardTypeWords =
    new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "artifact", "enchantment", "land",
    };

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    var colors = new List<string>();
    var cardTypes = new List<string>();
    var subtypes = new List<string>();

    // Walk the spec words, classifying each as a color, a card type, a connective
    // ("and"), or a creature subtype (anything else — oracle text capitalizes
    // subtypes, CR 205.3m).
    foreach (var rawWord in match.Groups["spec"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
    {
      var word = rawWord.Trim();
      if (word.Length == 0 || string.Equals(word, "and", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      if (_colorCodes.TryGetValue(word, out var code))
      {
        colors.Add(code);
      }
      else if (_cardTypeWords.Contains(word))
      {
        cardTypes.Add(word.ToLowerInvariant());
      }
      else
      {
        // A creature subtype, e.g. "Bird", "Beast". Title-case to match the
        // ObjectFilter.Subtypes convention.
        subtypes.Add(char.ToUpperInvariant(word[0]) + word[1..]);
      }
    }

    // The matched head noun "creature" is the added card type (CR 205.2). Restated
    // non-creature types from the spec precede it.
    cardTypes.Add("creature");

    var gainedAbilities = new List<Ability>();
    if (match.Groups["kw"].Success)
    {
      var keyword = match.Groups["kw"].Value.Trim();
      var ability = ActivatedRuleHelpers.BuildGrantedKeywordAbility(keyword);
      if (ability is null)
      {
        // Keyword not yet modeled — surface as unparsed rather than dropping it.
        return null;
      }
      gainedAbilities.Add(ability);
    }

    return new BecomesCreatureEffect
    {
      Subject = ObjectReference.Self(),
      Power = ParsePT(match.Groups["p"].Value),
      Toughness = ParsePT(match.Groups["t"].Value),
      Colors = colors,
      CardTypes = cardTypes,
      AddedSubtypes = subtypes,
      GainedAbilities = gainedAbilities,
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }

  // Animate P/T is a fixed literal ("2/2") or a variable ("X/X").
  private static Quantity ParsePT(string token) =>
    string.Equals(token, "X", StringComparison.OrdinalIgnoreCase)
      ? new VariableQuantity { Name = "X" }
      : LiteralQuantity.Of(int.Parse(token));
}
