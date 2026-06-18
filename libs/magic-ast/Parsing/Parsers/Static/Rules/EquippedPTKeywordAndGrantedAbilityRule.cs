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
/// "Equipped creature gets +N/+M and has [keyword] and "[quoted ability]"" — the P/T
/// buff plus a bare keyword grant and a quoted ability grant on the equipped creature.
///
/// <para>
/// Canonical example: The Reaver Cleaver — "Equipped creature gets +1/+1 and has trample
/// and 'Whenever this creature deals combat damage to a player or planeswalker, create that
/// many Treasure tokens.'" This is a three-part continuous ability:
/// <list type="bullet">
///   <item>P/T modification (layer 7c — CR 613.4c)</item>
///   <item>Keyword ability grant (layer 6 — CR 613.1f) — the bare keyword (e.g. trample)</item>
///   <item>Triggered ability grant (layer 6 — CR 613.1f) — the quoted triggered ability</item>
/// </list>
/// All three effects apply to the equipped creature
/// (<see cref="ObjectReferenceKind.EnchantedOrEquipped"/>) and are always-on (static, no
/// Duration).
/// </para>
///
/// <para>
/// Rule 702.6 (Equipment): an Equipment's static abilities apply to the equipped creature.
/// Rule 613.1 (layer ordering): P/T and keyword/ability grants are applied in strict layer
/// order; MAST records the oracle text descriptively — layer ordering is engine territory.
/// </para>
///
/// <para>
/// The quoted body is dispatched to the <see cref="TriggeredAbilityParser"/> first,
/// falling back to <see cref="ActivatedAbilityParser"/>, mirroring the sibling
/// <see cref="EnchantedPTAndGrantedAbilityRule"/>. Priority 968 (above
/// <see cref="EnchantedPTAndGrantedAbilityRule"/> at 967) so the three-part keyword+quoted
/// form is recognised before the two-part quoted form could attempt to match it.
/// </para>
/// </summary>
[StaticRule(Priority = 968)]
public sealed class EquippedPTKeywordAndGrantedAbilityRule : IStaticRule
{
  private readonly OracleTokenizer _tokenizer = new();

  // "Equipped creature gets +N/+M and has <keyword> and "<body>"."
  // <keyword> is a bare lowercase word or two-word phrase (e.g. "first strike").
  // The body is captured verbatim between curly or straight quotes.
  private static readonly Regex _pattern = new(
    @"^\s*Equipped\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+and\s+has\s+(?<kw>[a-z][a-z ]*?)\s+and\s+[""""](?<body>[^""""]+)[""""]\.?\s*$",
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

    var kw = match.Groups["kw"].Value.Trim().ToLowerInvariant();
    var body = match.Groups["body"].Value.Trim();
    if (body.Length == 0)
    {
      return null;
    }

    var grantedKeyword = StaticRuleHelpers.MapKeywordToStaticAbility(kw);
    if (grantedKeyword is null)
    {
      return null;
    }

    var grantedAbility = TryParseGrantedBody(body);
    if (grantedAbility is null)
    {
      return null;
    }

    var target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new CompositeEffect
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
                GainedAbility = grantedKeyword,
              },
              new GainAbilityEffect
              {
                Target = target,
                GainedAbility = grantedAbility,
              },
            ],
          },
        ],
      },
    ];
  }

  /// <summary>
  /// Parses the quoted body — a triggered ability first, falling back to an activated
  /// ability. Returns null when neither recognises the body so the caller surfaces the gap.
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
