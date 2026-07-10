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
/// "Equipped creature has "[quoted ability]" and "[quoted ability]"" — a bare grant
/// (no P/T buff, no bare keyword) of TWO independently-quoted abilities to the
/// equipped creature. Paradigm card: Thornbite Staff — "Equipped creature has '{2},
/// {T}: This creature deals 1 damage to any target' and 'Whenever a creature dies,
/// untap this creature.'"
///
/// <para>
/// Rule 702.6 (Equipment): an Equipment's static ability grants the equipped
/// creature the quoted abilities. Rule 113.3: an object can gain an ability from an
/// effect. This is a <b>static</b> continuous effect (layer 6, CR 613.1f) — the
/// grant persists for as long as the Equipment remains attached, so there is no
/// Duration node. Each quoted body is itself a full ability (one activated, CR
/// 602.1; one triggered, CR 603.2 here) — both land as structured
/// <see cref="GainAbilityEffect"/> nodes side by side in the static ability's flat
/// <c>Effects</c> list (mirroring the Blitz "it gains haste and '...'" shape, which
/// also emits two sibling <see cref="GainAbilityEffect"/>s with no composite
/// wrapper when there is no P/T buff to combine with).
/// </para>
///
/// <para>
/// Each quoted body is dispatched to <see cref="ActivatedAbilityParser"/> first,
/// falling back to <see cref="TriggeredAbilityParser"/> — mirroring the sibling
/// <see cref="EquippedCreatureHasQuotedAbilityRule"/>'s single-quote dispatch order.
/// Returns null (surfacing the gap honestly) when either body isn't recognised by
/// either parser.
/// </para>
///
/// <para>
/// Priority 997 — above <see cref="EnchantedHasGrantedTriggeredAbilityRule"/> (996)
/// and <see cref="GrantedAbilityRule"/> (995) so the two-quote compound form is
/// tried first; those single-quote rules' end-of-line anchors already fail to match
/// a second trailing "and '...'" clause, so this ordering is not load-bearing for
/// correctness, only for match-first-attempt efficiency.
/// </para>
/// </summary>
[StaticRule(Priority = 997)]
public sealed class EquippedCreatureHasTwoQuotedAbilitiesRule : IStaticRule
{
  private readonly OracleTokenizer _tokenizer = new();

  // "Equipped creature has "<body1>" and "<body2>"."
  // Four straight quotes in the verbatim string, wrapped in a character class,
  // collapse (via C#'s "" -> " unescaping) to a charset containing a single "
  // twice — i.e. [""] — which matches exactly one literal " character. Mirrors
  // GrantedAbilityRule's correct quote-boundary encoding (NOT the bare, unbracketed
  // """" used by EquippedCreatureHasQuotedAbilityRule, which requires two
  // consecutive " characters and so never matches real single-quoted oracle text).
  private static readonly Regex _pattern = new(
    @"^\s*Equipped\s+creature\s+has\s+[""""](?<body1>[^""""]+)[""""]\s+and\s+[""""](?<body2>[^""""]+)[""""]\.?\s*$",
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

    var body1 = match.Groups["body1"].Value.Trim();
    var body2 = match.Groups["body2"].Value.Trim();
    if (body1.Length == 0 || body2.Length == 0)
    {
      return null;
    }

    var grantedAbility1 = TryParseGrantedBody(body1);
    if (grantedAbility1 is null)
    {
      // The first body's shape isn't yet supported — surface the gap honestly.
      return null;
    }

    var grantedAbility2 = TryParseGrantedBody(body2);
    if (grantedAbility2 is null)
    {
      // The second body's shape isn't yet supported — surface the gap honestly.
      return null;
    }

    var target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new GainAbilityEffect { Target = target, GainedAbility = grantedAbility1 },
          new GainAbilityEffect { Target = target, GainedAbility = grantedAbility2 },
        ],
      },
    ];
  }

  /// <summary>
  /// Parses a quoted body — activated ability first (the common Equipment grant
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
