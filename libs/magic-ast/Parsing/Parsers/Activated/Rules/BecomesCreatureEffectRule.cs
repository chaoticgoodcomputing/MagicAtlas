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
/// continuous effect (CR 611.1 — "A continuous effect modifies characteristics of
/// objects ... for a fixed or indefinite period.") that sets card types/subtypes
/// (CR 205), power/toughness (CR 208), colors (CR 105), and grants a keyword ability
/// (CR 113.6). Layer/timestamp ordering (CR 613) is engine territory and is not
/// modeled.
/// </para>
///
/// <para>
/// <b>Word order.</b> The Keyrune/Monument form puts the duration LAST ("...with
/// flying until end of turn"). The "manland" animate (Stirring Wildwood, Restless
/// Vents) puts it FIRST ("Until end of turn, this land becomes a 3/4 ... creature
/// with reach"). Both are the same continuous effect; the rule accepts either word
/// order and normalizes to <c>Duration: untilEndOfTurn</c>.
/// </para>
///
/// <para>
/// <b>"It's still a [type]" retention.</b> Per CR 205.1b — "Some effects change an
/// object's card type, supertype, or subtype but specify that the object retains a
/// prior card type ... This rule applies to effects that ... state that something is
/// 'still a [type...]'" — the animate is ADDITIVE: the source keeps its prior card
/// type and gains <c>creature</c>. CR 305.7 says setting/animating a land "doesn't
/// add or remove any card types ... it keeps its land types". So for the manlands the
/// retained <c>land</c> card type sits ahead of the added <c>creature</c> in
/// <see cref="BecomesCreatureEffect.CardTypes"/>. The retained type is derived from
/// the subject noun ("this <b>land</b> becomes..."); the trailing reminder sentence
/// "It's still a land." is consumed as confirmation, NOT emitted as a separate effect.
/// </para>
///
/// <para>
/// Examples:
/// <list type="bullet">
///   <item>Azorius Keyrune — "This artifact becomes a 2/2 white and blue Bird
///   artifact creature with flying until end of turn."</item>
///   <item>Gruul Keyrune — "This artifact becomes a 3/2 red and green Beast artifact
///   creature with trample until end of turn."</item>
///   <item>Stirring Wildwood — "Until end of turn, this land becomes a 3/4 green and
///   white Elemental creature with reach. It's still a land." → CardTypes
///   ["land","creature"].</item>
///   <item>Restless Vents — "Until end of turn, this land becomes a 2/3 black and red
///   Insect creature with menace. It's still a land."</item>
/// </list>
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 985)]
public sealed class BecomesCreatureEffectRule : IActivatedEffectRule
{
  // Trailing-duration form (Keyrune): "This artifact becomes a 2/2 white and blue Bird
  // artifact creature with flying until end of turn". The <spec> group is the run of
  // words between the P/T box and the literal "creature" head noun: colors ("white and
  // blue"), the subtype ("Bird"), and any non-creature card types ("artifact"). We
  // classify those words afterward.
  private static readonly Regex _trailingDurationPattern = new(
    @"^This\s+\w+\s+becomes\s+a\s+(?<p>\d+|X)/(?<t>\d+|X)\s+(?<spec>.+?)\s+creature(?:\s+with\s+(?<kw>[a-z]+(?:\s+strike)?))?\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Leading-duration form (manland): "Until end of turn, this land becomes a 3/4 green
  // and white Elemental creature with reach. It's still a land". The duration precedes
  // the clause; an optional trailing "It's still a [type]" retention reminder
  // (CR 205.1b) may follow and is consumed, not emitted separately. <subj> captures the
  // source noun ("land") whose card type is RETAINED additively (CR 305.7).
  private static readonly Regex _leadingDurationPattern = new(
    @"^Until\s+end\s+of\s+turn,\s+This\s+(?<subj>\w+)\s+becomes\s+a\s+(?<p>\d+|X)/(?<t>\d+|X)\s+(?<spec>.+?)\s+creature(?:\s+with\s+(?<kw>[a-z]+(?:\s+strike)?))?(?:\.\s*It['’]s\s+still\s+a\s+(?<retain>\w+))?$",
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

    // Trailing-duration (Keyrune) first, then leading-duration (manland). Both
    // describe the same continuous effect (CR 611.1) — only word order differs.
    var match = _trailingDurationPattern.Match(trimmed);
    string? retainedType = null;
    if (!match.Success)
    {
      match = _leadingDurationPattern.Match(trimmed);
      if (!match.Success)
      {
        return null;
      }

      // CR 205.1b / CR 305.7: the animate is additive — the source keeps its prior
      // card type ("this LAND becomes...") and gains "creature". Derive the retained
      // type from the subject noun; the optional "It's still a land" reminder is the
      // confirmation of that retention and is consumed here, not emitted as an effect.
      var subject = match.Groups["subj"].Value.Trim().ToLowerInvariant();
      if (_cardTypeWords.Contains(subject))
      {
        retainedType = subject;
      }
    }

    var colors = new List<string>();
    var cardTypes = new List<string>();
    var subtypes = new List<string>();

    // The retained card type (CR 205.1b) leads the list, ahead of the added
    // "creature" head noun — e.g. ["land","creature"] for an animated land.
    if (retainedType is not null)
    {
      cardTypes.Add(retainedType);
    }

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
