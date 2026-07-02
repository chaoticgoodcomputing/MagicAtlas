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
/// "Equipped creature has "[quoted ability]"" — grants a quoted activated ability to
/// the equipped creature with no accompanying P/T modifier. Paradigm card: KHM
/// Toralf's Hammer — "Equipped creature has '{1}{R}, {T}, Unattach Toralf's Hammer:
/// It deals 3 damage to any target. Return Toralf's Hammer to its owner's hand.'"
///
/// <para>
/// This is a <b>static</b> continuous effect (CR 611.1). The grant persists for as
/// long as the Equipment remains attached — there is no Duration node.
/// </para>
///
/// <para>
/// The quoted body is dispatched to <see cref="ActivatedAbilityParser"/> first (the
/// common Equipment grant shape), falling back to <see cref="TriggeredAbilityParser"/>.
/// Returns null (surfacing the gap honestly) when neither recognises the body.
/// </para>
///
/// <para>
/// Priority 964 — below <see cref="EnchantedPTAndGrantedAbilityRule"/> (967) and
/// <see cref="EquippedPTKeywordAndGrantedAbilityRule"/> (968) so those compound forms
/// are recognised first. Only the bare "Equipped creature has [quoted]" form falls
/// through to this rule.
/// </para>
/// </summary>
[StaticRule(Priority = 964)]
public sealed class EquippedCreatureHasQuotedAbilityRule : IStaticRule
{
  private readonly OracleTokenizer _tokenizer = new();

  // "Equipped creature has "[quoted body]"."
  // Four straight quotes in the verbatim string = ""+"" = two literal " chars in the
  // regex, giving the charset ["""] = matches ".
  private static readonly Regex _pattern = new(
    @"^\s*Equipped\s+creature\s+has\s+""""(?<body>[^""""]+)""""\s*\.?\s*$",
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

    var grantedAbility = TryParseGrantedBody(body);
    if (grantedAbility is null)
    {
      // The body's shape isn't yet supported — surface the gap honestly.
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
  /// Parses the quoted body — activated ability first (the common Equipment grant
  /// shape), falling back to triggered. Returns null when neither recognises the body.
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

    // Try activated first — Equipment grants are typically activated abilities.
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
