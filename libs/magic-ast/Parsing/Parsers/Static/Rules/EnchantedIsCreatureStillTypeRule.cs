namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Enchanted [type] is a [P/T] [colors] [subtype] creature. It's still a [type]."
/// — the Zendikon cycle's "animate the enchanted land" template: a single always-on
/// static continuous effect (CR 613) that turns the enchanted permanent into a
/// fully-specified creature (power/toughness, colors, a creature subtype), while
/// retaining the permanent's prior card type via the trailing "It's still a [type]"
/// sentence (CR 205.1b).
///
/// <para>
/// Sibling of <see cref="EnchantedIsCreatureWithBasePTRule"/> (Ensoul Artifact's
/// "...with base power and toughness P/T in addition to its other types" shape) and
/// <see cref="AllLandsAreCreaturesStillLandsRule"/> (Nature's Revolt's "All lands are
/// P/T creatures that are still lands" shape) — all three emit the same
/// <see cref="BecomesCreatureEffect"/> node. This rule differs from both: it states a
/// full color + creature-subtype spec (not just a bare P/T box) inline in the head
/// clause, and states the retention as a SEPARATE trailing sentence ("It's still a
/// land.") rather than an inline "in addition to"/"that are still" clause — the same
/// trailing-retention shape the manland activated animate
/// (<see cref="MagicAST.Parsing.Parsers.Activated.Rules.BecomesCreatureEffectRule"/>)
/// uses, but here unconditional/static (no cost, no "until end of turn" duration) and
/// scoped to the single enchanted permanent
/// (<see cref="ObjectReferenceKind.EnchantedOrEquipped"/>) rather than the source
/// itself.
/// </para>
///
/// <para>
/// <b>"It's still a [type]" retention (CR 205.1b).</b> "Some effects change an
/// object's card type ... but specify that the object retains a prior card type ...
/// This rule applies to effects that ... state that something is 'still a
/// [type...]'." CR 305.7: an animated land "doesn't add or remove any card types ...
/// it keeps its land types." So the retained type (here "land", matching the Aura's
/// own "Enchant land" restriction) sits ahead of the added "creature" in
/// <see cref="BecomesCreatureEffect.CardTypes"/>. The trailing sentence is consumed
/// as confirmation, not emitted as a separate effect.
/// </para>
///
/// <para>
/// Canonical card: Vastwood Zendikon — "Enchanted land is a 6/4 green Elemental
/// creature. It's still a land." → Power/Toughness 6/4, Colors ["G"], AddedSubtypes
/// ["Elemental"], CardTypes ["land","creature"].
/// </para>
///
/// <para>
/// Anchored (^…$) to the exact "Enchanted [type] is a P/T [spec] creature. It's still
/// a [type]" shape so it cannot collide with <see cref="EnchantedIsCreatureWithBasePTRule"/>
/// (requires the literal "with base power and toughness ... in addition to its other
/// types" wording, absent here) or <see cref="EnchantedLandIsSubtypeRule"/> (a bare
/// "Enchanted land is a(n) [BasicLandType]" subtype declaration, no P/T box).
/// </para>
///
/// Rule 613 (Layer System / continuous effects); Rule 205.1b, 205.2 (card types);
/// Rule 208.3 (power/toughness); Rule 105 (color); Rule 305.7 (animated lands retain
/// land types); Rule 303.4c / 702.5 ("enchanted [type]" refers to the attached
/// permanent).
/// </summary>
[StaticRule(Priority = 971)]
public sealed class EnchantedIsCreatureStillTypeRule : IStaticRule
{
  // "Enchanted land is a 6/4 green Elemental creature. It's still a land." <subj> is
  // the retained card type (matches the Aura's own "Enchant [type]" line); <spec> is
  // the run of words between the P/T box and the literal "creature" head noun (colors
  // + subtype, classified below); <retain> is the trailing confirmation sentence's
  // type word (consumed, not separately emitted).
  private static readonly Regex _pattern = new(
    @"^\s*Enchanted\s+(?<subj>artifact|land|creature|permanent|enchantment|planeswalker)\s+is\s+a\s+(?<p>\d+|X)/(?<t>\d+|X)\s+(?<spec>.+?)\s+creature\.\s*It['’]s\s+still\s+a\s+(?<retain>\w+)\.?\s*$",
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

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var retainedType = match.Groups["subj"].Value.ToLowerInvariant();

    var colors = new List<string>();
    var subtypes = new List<string>();

    // Walk the spec words, classifying each as a color, a connective ("and"), or a
    // creature subtype (anything else — oracle text capitalizes subtypes, CR 205.3m).
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
      else
      {
        subtypes.Add(char.ToUpperInvariant(word[0]) + word[1..]);
      }
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new BecomesCreatureEffect
          {
            Subject = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
            Power = ParsePT(match.Groups["p"].Value),
            Toughness = ParsePT(match.Groups["t"].Value),
            Colors = colors,
            CardTypes = [retainedType, "creature"],
            AddedSubtypes = subtypes,
            GainedAbilities = [],
          },
        ],
      },
    ];
  }

  // Animate P/T is a fixed literal ("6/4") or a variable ("X/X").
  private static Quantity ParsePT(string token) =>
    string.Equals(token, "X", StringComparison.OrdinalIgnoreCase)
      ? new VariableQuantity { Name = "X" }
      : LiteralQuantity.Of(int.Parse(token));
}
