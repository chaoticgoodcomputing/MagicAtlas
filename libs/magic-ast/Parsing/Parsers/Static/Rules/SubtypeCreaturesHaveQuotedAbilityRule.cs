namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Parsers;
using MagicAST.Parsing.Tokens;
using Superpower.Model;

/// <summary>
/// "&lt;Subtype&gt; creatures you control have "[quoted ability]"" — grants a quoted
/// ability (typically an activated mana ability) to every creature of the named
/// subtype the controller controls. Paradigm card: Manaweft Sliver — "Sliver
/// creatures you control have '{T}: Add one mana of any color.'"
///
/// <para>
/// This is a <b>static</b> continuous effect (CR 611.1: "A continuous effect
/// modifies characteristics of objects … for a fixed or indefinite period.";
/// CR 113.3: "Some effects and static abilities can grant an object an activated
/// ability."). The grant persists for as long as the source remains on the
/// battlefield with the ability — there is no Duration node.
/// </para>
///
/// <para>
/// The quoted body is dispatched to <see cref="ActivatedAbilityParser"/> first (the
/// common tribal-lord grant shape — a "{T}: Add …" mana ability), falling back to
/// <see cref="TriggeredAbilityParser"/>. Returns null (surfacing the gap honestly)
/// when neither recognises the body, mirroring
/// <see cref="EquippedCreatureHasQuotedAbilityRule"/>.
/// </para>
///
/// <para>
/// Priority 974 — above <see cref="ControlledFilterHaveKeywordListRule"/> (973) so
/// this more-specific quoted-ability shape is tried first, though in practice the
/// sibling bare-keyword rules already decline on their own: their keyword capture
/// groups are restricted to lowercase-letter/space charsets that cannot match a
/// leading quote character, so dispatch order does not change behaviour either way.
/// Anchored (^…$) pattern prevents substring matches against sibling clauses.
/// </para>
/// </summary>
[StaticRule(Priority = 974)]
public sealed class SubtypeCreaturesHaveQuotedAbilityRule : IStaticRule
{
  private readonly OracleTokenizer _tokenizer = new();

  // "<Subtype> creatures you control have "[quoted body]"."
  // Two straight quotes in the verbatim C# string literal (@"...""...") escape to
  // ONE literal " character in the resulting regex, so a bare "" here matches a
  // single quote mark around the granted ability's oracle text.
  private static readonly Regex _pattern = new(
    @"^\s*(?<sub>[A-Z][a-z]+)\s+creatures\s+you\s+control\s+have\s+""(?<body>[^""]+)""\s*\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var body = match.Groups["body"].Value.Trim();
    if (body.Length == 0)
    {
      return null;
    }

    var grantedAbility = TryParseGrantedBody(body);
    if (grantedAbility is null)
    {
      // The body's shape isn't yet supported — surface the gap honestly.
      return null;
    }

    var subtype = match.Groups["sub"].Value;
    var target = new ObjectReference
    {
      Kind = ObjectReferenceKind.Each,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
      },
    };

    return
    [
      new StaticAbility
      {
        Effects = [new GainAbilityEffect
        {
          Target = target,
          GainedAbility = grantedAbility,
        }],
      },
    ];
  }

  /// <summary>
  /// Parses the quoted body — activated ability first (the common tribal-lord mana
  /// grant shape), falling back to triggered. Returns null when neither recognises
  /// the body.
  /// </summary>
  private Ability? TryParseGrantedBody(string body)
  {
    var tokenResult = _tokenizer.TryTokenize(body);
    var tokens = tokenResult.HasValue ? tokenResult.Value : new TokenList<OracleToken>([]);

    var innerClause = new OracleClause
    {
      Tokens = tokens,
      RawText = body,
      SourceSpan = new MagicAST.AST.TextSpan(0, body.Length),
    };

    // Try activated first — tribal-lord grants are typically "{T}: Add …" mana abilities.
    var activated = new ActivatedAbilityParser().TryParse(
      innerClause,
      new ClauseClassification { Kind = AbilityKind.Activated, Confidence = 1.0 }
    );
    if (activated is not null)
    {
      return activated;
    }

    // Fall back to triggered for triggered-ability grants.
    return new TriggeredAbilityParser().TryParse(
      innerClause,
      new ClauseClassification { Kind = AbilityKind.Triggered, Confidence = 1.0 }
    );
  }
}
