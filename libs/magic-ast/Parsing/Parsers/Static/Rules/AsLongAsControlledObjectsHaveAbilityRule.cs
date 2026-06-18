namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Parsers;
using MagicAST.Parsing.Tokens;
using Superpower.Model;

/// <summary>
/// Recognises the leading "As long as [condition], [card-types] you control have
/// &quot;[quoted activated ability]&quot;." form — a conditional continuous grant
/// of an activated ability to a set of controlled permanents.
///
/// <para>
/// The World Tree's third ability is the canonical example:
/// "As long as you control six or more lands, lands you control have
/// '{T}: Add one mana of any color.'"
/// </para>
///
/// <para>
/// Shape: the leading "As long as [condition]" block gates the grant duration
/// (<see cref="AsLongAsDuration"/>). The main clause "[types] you control have
/// [quoted ability]" becomes a <see cref="GainAbilityEffect"/> targeting each
/// permanent of the listed card type(s) controlled by you. The quoted body is
/// dispatched to <see cref="ActivatedAbilityParser"/> to produce a nested
/// <see cref="ActivatedAbility"/>.
/// </para>
///
/// <para>
/// CR 604.1: "Static abilities do something all the time rather than being
/// activated or triggered." CR 604.2: "Static abilities create continuous effects,
/// some of which are … ability-granting effects." CR 611.1: "A continuous effect
/// modifies characteristics of objects … for a fixed or indefinite period."
/// CR 602.1: Activated abilities have cost and effect ("[Cost]: [Effect]").
/// </para>
///
/// <para>
/// Priority 969 — above <see cref="AsLongAsStaticGrantRule"/> (968) so this rule
/// claims the "lands you control have [quoted]" clause before the generic rule
/// attempts its suffix-pattern and fails. The regex anchors are ^…$ so there is no
/// substring-overlap risk with sibling rules.
/// </para>
/// </summary>
[StaticRule(Priority = 969)]
public sealed class AsLongAsControlledObjectsHaveAbilityRule : IStaticRule
{
  private readonly OracleTokenizer _tokenizer = new();

  // Matches: "As long as <cond>, <type>s you control have "<body>"."
  // Accepts straight ASCII double-quote (0x22) and the two Unicode curly-quote
  // variants (U+201C / U+201D) used in oracle text.
  // <cond>: everything between "As long as " and the comma (no comma within).
  // <type>: the (potentially plural) card-type noun before " you control".
  // <body>: the quoted ability text.
  private static readonly Regex _patternAscii = new(
    "^\\s*As\\s+long\\s+as\\s+(?<cond>[^,]+),\\s*"
    + "(?<type>[A-Za-z]+s?)\\s+you\\s+control\\s+have?\\s+"
    + "[\"\\u201C\\u201D](?<body>[^\"\\u201C\\u201D]+)[\"\\u201C\\u201D]\\.?\\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known card-type plural-to-singular mapping (oracle uses plural in this shape).
  private static readonly Dictionary<string, string> _pluralToSingular = new(
    StringComparer.OrdinalIgnoreCase)
  {
    ["creatures"] = "creature",
    ["artifacts"] = "artifact",
    ["enchantments"] = "enchantment",
    ["lands"] = "land",
    ["planeswalkers"] = "planeswalker",
    ["permanents"] = "permanent",
    ["spells"] = "spell",
    ["instants"] = "instant",
    ["sorceries"] = "sorcery",
    // Singular forms too (for safety).
    ["creature"] = "creature",
    ["artifact"] = "artifact",
    ["enchantment"] = "enchantment",
    ["land"] = "land",
    ["planeswalker"] = "planeswalker",
    ["permanent"] = "permanent",
  };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var m = _patternAscii.Match(clause.RawText);
    if (!m.Success)
    {
      return null;
    }

    var condText = m.Groups["cond"].Value.Trim();
    var typeText = m.Groups["type"].Value.Trim();
    var body = m.Groups["body"].Value.Trim();

    if (body.Length == 0)
    {
      return null;
    }

    // Map the card type noun to a singular card-type string.
    if (!_pluralToSingular.TryGetValue(typeText, out var singularType))
    {
      // Unknown card type — decline and let the fallback handle it.
      return null;
    }

    // Parse the condition ("you control six or more lands" → CountCondition).
    var condition = ConditionParser.Parse(condText);
    var duration = new AsLongAsDuration { Condition = condition };

    // Build the grant target: "each <type> you control".
    var grantTarget = new ObjectReference
    {
      Kind = ObjectReferenceKind.Each,
      Filter = new ObjectFilter
      {
        CardTypes = [singularType],
        Controller = ControllerFilter.You,
      },
    };

    // Parse the quoted body as an activated ability.
    var grantedAbility = TryParseActivatedBody(body);
    if (grantedAbility is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new GainAbilityEffect
          {
            Target = grantTarget,
            GainedAbility = grantedAbility,
            Duration = duration,
          },
        ],
      },
    ];
  }

  private Ability? TryParseActivatedBody(string body)
  {
    var tokenResult = _tokenizer.TryTokenize(body);
    var tokens = tokenResult.HasValue
      ? tokenResult.Value
      : new TokenList<OracleToken>([]);

    var innerClause = new OracleClause
    {
      Tokens = tokens,
      RawText = body,
      SourceSpan = new MagicAST.AST.TextSpan(0, body.Length),
    };
    var innerClassification = new ClauseClassification
    {
      Kind = AbilityKind.Activated,
      Confidence = 1.0,
    };

    return new ActivatedAbilityParser().TryParse(innerClause, innerClassification);
  }
}
