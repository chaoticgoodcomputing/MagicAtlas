namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Parsers;
using MagicAST.Parsing.Tokens;
using Superpower.Model;

/// <summary>
/// "Other [Color] creatures you control have "[quoted ability]"." — a colour-filtered
/// anthem that grants a fully-structured quoted ability (Unctus, Grand Metatect:
/// <c>"Other blue creatures you control have \"Whenever this creature becomes tapped,
/// draw a card, then discard a card.\""</c>). CR 611.3: a continuous effect generated
/// by a static ability may grant an ability to other permanents.
///
/// <para>
/// Sibling of <see cref="GrantedAbilityRule"/> (which recognises "Enchanted/Equipped
/// creature", "[Color] [cardtype]s you control", "[Cardtype]s you control", "All
/// [Subtype]s", and "[Subtype]s you control" grant subjects, but whose
/// <c>ClassifyGrantTarget</c> branches are all anchored WITHOUT an "Other " prefix —
/// so a self-excluding colour-tribal subject like "Other blue creatures you control"
/// falls through it) and of <see cref="EnchantedPTAndGrantedAbilityRule"/> (whose
/// quoted-body dispatch order — <see cref="TriggeredAbilityParser"/> first,
/// <see cref="ActivatedAbilityParser"/> fallback — is mirrored here, since the
/// recurring quoted-grant shape is a triggered ability). The "Other " prefix marks
/// CR 109.5's self-exclusion (the source itself is not among the OTHER creatures it
/// affects), so the target filter carries <c>ExcludeSelf = true</c>, mirroring
/// <c>TribalAnthemModifyPTRule</c>'s identical "Other [Subtype] creatures you control"
/// self-exclusion — but scoped by colour (<see cref="ObjectFilter.Colors"/>) instead
/// of subtype.
/// </para>
///
/// <para>
/// The self-reference inside the quoted body ("this creature") resolves to the
/// granted ability's own source (CR 109) via
/// <see cref="MagicAST.Parsing.Parsers.Triggered.TriggeredRuleHelpers.ParseObjectFilter"/>,
/// which marks it <c>IsSelf</c> — NOT a second "blue creature" filter layered onto the
/// trigger.
/// </para>
/// </summary>
[StaticRule(Priority = 60)]
public sealed class OtherColorCreaturesGrantQuotedTriggeredAbilityRule : IStaticRule
{
  private readonly OracleTokenizer _tokenizer = new();

  // "Other [Color] creatures you control have "<body>"." — straight-quote body
  // capture (matches the oracle-text quoting convention for this shape); nested
  // quotes inside the body are out of scope for this first cut.
  private static readonly Regex _pattern = new(
    @"^\s*Other\s+(?<color>White|Blue|Black|Red|Green)\s+creatures\s+you\s+control\s+have\s+""(?<body>[^""]+)""\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _colorCodes =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["White"] = "W",
      ["Blue"] = "U",
      ["Black"] = "B",
      ["Red"] = "R",
      ["Green"] = "G",
    };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    if (!_colorCodes.TryGetValue(match.Groups["color"].Value, out var colorCode))
    {
      return null;
    }

    var body = match.Groups["body"].Value.Trim();
    if (body.Length == 0)
    {
      return null;
    }

    // The quoted body's real absolute offset within the card's oracle text — NOT
    // the 0-based offset of `body` within itself. `match.Groups["body"].Index` is
    // relative to `clause.RawText`, but `.Trim()` above may have shifted the true
    // content start further still. Locating the (already trimmed) body inside
    // `clause.RawText` and adding `clause.SourceSpan.Start` recovers the true
    // absolute start, so every span the inner parser derives from
    // `innerClause.SourceSpan.Start` (see TriggeredAbilityParser/ActivatedAbilityParser)
    // lands correctly rebased instead of 0-based-and-disconnected.
    var bodyOffsetInClause = clause.RawText.IndexOf(body, System.StringComparison.Ordinal);
    var absoluteBodyStart = clause.SourceSpan.Start + Math.Max(bodyOffsetInClause, 0);

    var grantedAbility = TryParseGrantedBody(body, absoluteBodyStart);
    if (grantedAbility is null)
    {
      // The body's shape isn't yet supported — surface the gap via the fallback.
      return null;
    }

    var target = new ObjectReference
    {
      Kind = ObjectReferenceKind.Each,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Colors = [colorCode],
        Controller = ControllerFilter.You,
        ExcludeSelf = true,
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
  /// Parses the quoted body — a triggered ability first (the recurring
  /// grant-a-triggered-ability shape), falling back to an activated ability
  /// (mirroring <see cref="EnchantedPTAndGrantedAbilityRule.TryParseGrantedBody"/>).
  /// Returns null when neither recognises the body so the caller surfaces the gap.
  /// </summary>
  /// <param name="body">The quoted ability text, trimmed.</param>
  /// <param name="absoluteBodyStart">
  /// The body's real absolute offset within the card's oracle text — the basis every
  /// span the inner parser derives is rebased from (see caller).
  /// </param>
  private Ability? TryParseGrantedBody(string body, int absoluteBodyStart)
  {
    var tokenResult = _tokenizer.TryTokenize(body);
    var tokens = tokenResult.HasValue ? tokenResult.Value : new TokenList<OracleToken>([]);

    var innerClause = new OracleClause
    {
      Tokens = tokens,
      RawText = body,
      SourceSpan = new MagicAST.AST.TextSpan(absoluteBodyStart, body.Length),
    };

    var triggered = new TriggeredAbilityParser().TryParse(
      innerClause,
      new ClauseClassification { Kind = AbilityKind.Triggered, Confidence = 1.0 }
    );
    if (triggered is not null)
    {
      return triggered;
    }

    return new ActivatedAbilityParser().TryParse(
      innerClause,
      new ClauseClassification { Kind = AbilityKind.Activated, Confidence = 1.0 }
    );
  }
}
