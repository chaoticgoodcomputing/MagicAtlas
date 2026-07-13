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
/// "(Enchanted|Equipped) creature has "[quoted TRIGGERED ability]"" — the bare
/// grant (no P/T buff) of a full triggered ability quoted in the Aura/Equipment's
/// oracle text (Sunbond: <c>Enchanted creature has "Whenever you gain life, put
/// that many +1/+1 counters on this creature."</c>). CR 113.3 (an object can gain
/// an ability from an effect) / CR 603.2 (triggered abilities are written "when,"
/// "whenever," or "at").
///
/// <para>
/// Sibling of <see cref="EnchantedPTAndGrantedAbilityRule"/> (the PT-plus-grant
/// composite shape) and <see cref="GrantedAbilityRule"/> (the generic grant rule,
/// whose <c>TryParseGrantedBody</c> dispatches ONLY to
/// <see cref="ActivatedAbilityParser"/>, so it cannot claim a quoted TRIGGERED
/// body). This rule instead dispatches the quoted body to
/// <see cref="TriggeredAbilityParser"/>; when the body isn't a triggered ability
/// this rule returns null so <see cref="GrantedAbilityRule"/>'s activated-ability
/// path continues to own the bare-grant activated-ability shape unchanged.
/// </para>
///
/// <para>
/// Priority 996 — one above <see cref="GrantedAbilityRule"/> (995) so this
/// triggered-ability shape is recognised before the generic grant rule (which
/// would otherwise match the same clause, fail to parse the quoted TRIGGERED body
/// via <see cref="ActivatedAbilityParser"/>, and return null — dropping the whole
/// ability to the fallback). When the quoted body is NOT a triggered ability, this
/// rule returns null and <see cref="GrantedAbilityRule"/> picks up the
/// activated-ability case exactly as before — collision-free.
/// </para>
/// </summary>
[StaticRule(Priority = 996)]
public sealed class EnchantedHasGrantedTriggeredAbilityRule : IStaticRule
{
  private readonly OracleTokenizer _tokenizer = new();

  // "(Enchanted|Equipped) creature has "<body>"." — anchored to the bare
  // Aura/Equipment grant subject only (no PT buff, no "and" compound — those
  // shapes are owned by EnchantedPTAndGrantedAbilityRule / EnchantedPTAndKeywordRule).
  private static readonly Regex _pattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+has\s+[""""](?<body>[^""""]+)[""""]\.?\s*$",
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

    var body = match.Groups["body"].Value.Trim();
    if (body.Length == 0)
    {
      return null;
    }

    var grantedAbility = TryParseTriggeredBody(body);
    if (grantedAbility is null)
    {
      // Not a triggered-ability body — leave this clause to GrantedAbilityRule's
      // activated-ability path (lower priority, tried next).
      return null;
    }

    var target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };

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
  /// Hands the quoted body off to <see cref="TriggeredAbilityParser"/>.
  /// </summary>
  private Ability? TryParseTriggeredBody(string body)
  {
    var tokenResult = _tokenizer.TryTokenize(body);
    var tokens = tokenResult.HasValue ? tokenResult.Value : new TokenList<OracleToken>([]);

    var innerClause = new OracleClause
    {
      Tokens = tokens,
      RawText = body,
      SourceSpan = new MagicAST.AST.TextSpan(0, body.Length),
    };

    return new TriggeredAbilityParser().TryParse(
      innerClause,
      new ClauseClassification { Kind = AbilityKind.Triggered, Confidence = 1.0 }
    );
  }
}
