namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Parsers;
using MagicAST.Parsing.Tokens;
using Superpower.Model;

/// <summary>
/// "(Enchanted|Equipped) creature gets +N/+M and has "[quoted ability]"" — the P/T
/// buff plus a granted <em>quoted ability</em> (Bear Umbra: <c>gets +2/+2 and has
/// "Whenever this creature attacks, untap all lands you control."</c>). Sibling of
/// <see cref="EnchantedPTAndKeywordRule"/>, which grants a bare <em>keyword</em>; this
/// rule grants the quoted body the Aura confers (CR 303.4 / 702.5 — the enchanted
/// permanent gains the quoted ability).
///
/// <para>
/// The quoted body is dispatched to the <see cref="TriggeredAbilityParser"/> first
/// (the recurring Aura shape grants a triggered ability), falling back to the
/// <see cref="ActivatedAbilityParser"/> (mirroring <see cref="GrantedAbilityRule"/>'s
/// hand-off). The self-reference inside the quoted body ("this creature") resolves to
/// the granted ability's own source (CR 109), so it is marked <c>IsSelf</c> by
/// <see cref="MagicAST.Parsing.Parsers.Triggered.TriggeredRuleHelpers.ParseObjectFilter"/>
/// — NOT to a separate "enchanted creature" filter.
/// </para>
///
/// <para>
/// Emits a <see cref="CompositeEffect"/> of [<see cref="ModifyPTEffect"/>,
/// <see cref="GainAbilityEffect"/>] both targeting
/// <see cref="ObjectReferenceKind.EnchantedOrEquipped"/> — the exact shape of
/// <see cref="EnchantedPTAndKeywordRule"/>, with a parsed ability in place of the
/// keyword's static expansion. Priority 967 (above <see cref="EnchantedPTAndDualKeywordRule"/>
/// at 966 and <see cref="EnchantedPTAndKeywordRule"/> at 965) so the quoted form is
/// recognised before the keyword forms attempt (and fail) to map the quoted body.
/// </para>
/// </summary>
[StaticRule(Priority = 967)]
public sealed class EnchantedPTAndGrantedAbilityRule : IStaticRule
{
  private readonly OracleTokenizer _tokenizer = new();

  // "(Enchanted|Equipped) creature gets +N/+M and has "<body>"."
  // The body is captured verbatim between the (curly or straight) quotes; nested
  // quotes inside the body are out of scope for this first cut.
  private static readonly Regex _pattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+and\s+has\s+[""""](?<body>[^""""]+)[""""]\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var psign = match.Groups["psign"].Value;
    var power = int.Parse(match.Groups["p"].Value);
    if (psign == "-") power = -power;

    var tsign = match.Groups["tsign"].Value;
    var toughness = int.Parse(match.Groups["t"].Value);
    if (tsign == "-") toughness = -toughness;

    var body = match.Groups["body"].Value.Trim();
    if (body.Length == 0)
    {
      return null;
    }

    // Rebase the quoted body's span onto its REAL absolute offset in the original
    // oracle text: `body` is only a substring of `clause.RawText`, so the naive
    // 0-based span the inner parser would otherwise stamp on the granted ability
    // (and its nested effects/triggers, which are computed relative to whatever
    // SourceSpan.Start we hand it) needs `clause.SourceSpan.Start` PLUS the body's
    // own offset within `clause.RawText` — not `clause.SourceSpan.Start` alone.
    var bodyOffsetInClause = clause.RawText.IndexOf(body, System.StringComparison.Ordinal);
    var bodyAbsoluteStart = clause.SourceSpan.Start + (bodyOffsetInClause >= 0 ? bodyOffsetInClause : 0);

    var grantedAbility = TryParseGrantedBody(body, bodyAbsoluteStart);
    if (grantedAbility is null)
    {
      // The body's shape isn't yet supported — surface the gap via the fallback.
      return null;
    }

    var target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };

    return
    [
      new StaticAbility
      {
        Effects = [new CompositeEffect
        {
          Effects =
          [
            new ModifyPTEffect
            {
              Target = target,
              PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
              ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
            },
            new GainAbilityEffect
            {
              Target = target,
              GainedAbility = grantedAbility,
            },
          ],
        }],
      },
    ];
  }

  /// <summary>
  /// Parses the quoted body — a triggered ability first (the recurring Aura grant
  /// shape), falling back to an activated ability (mirroring
  /// <see cref="GrantedAbilityRule.TryParseGrantedBody"/>). Returns null when neither
  /// recognises the body so the caller surfaces the gap.
  /// </summary>
  /// <param name="body">The quoted ability text, verbatim.</param>
  /// <param name="bodyAbsoluteStart">
  /// The body's real absolute offset into the original oracle text (NOT 0-based —
  /// the inner parsers compute every nested effect/cost span off this clause's
  /// SourceSpan.Start, so a wrong basis here silently corrupts every span the
  /// inner parser produces).
  /// </param>
  private Ability? TryParseGrantedBody(string body, int bodyAbsoluteStart)
  {
    var tokenResult = _tokenizer.TryTokenize(body);
    var tokens = tokenResult.HasValue ? tokenResult.Value : new TokenList<OracleToken>([]);

    var innerClause = new OracleClause
    {
      Tokens = tokens,
      RawText = body,
      SourceSpan = new MagicAST.AST.TextSpan(bodyAbsoluteStart, body.Length),
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
