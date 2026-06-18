namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Parses "Nontoken creatures you control have [keyword]." — a static continuous
/// effect that grants a keyword ability to every nontoken creature the controller
/// controls. This is the Rhythm of the Wild shape.
///
/// <para>
/// CR 702.136a (verbatim): "Riot is a static ability. 'Riot' means 'You may have
/// this permanent enter with an additional +1/+1 counter on it. If you don't, it
/// gains haste.'"
/// </para>
///
/// <para>
/// The "nontoken" qualifier maps to <c>IsToken = false</c> on the
/// <see cref="ObjectFilter"/> (CR 111.1: a token is not a card). The grant is a
/// continuous effect with no explicit duration — a static ability's continuous
/// effect lasts for as long as the source remains on the battlefield.
/// </para>
///
/// <para>
/// Priority 969 — above <see cref="BareKeywordGrantRule"/> (967) so this more-specific
/// nontoken-scoped shape fires before the broader filter arm would mis-match
/// "Nontoken creatures" as a subtype "Nontoken". Anchored pattern prevents substring
/// matches against more-specific siblings.
/// </para>
/// </summary>
[StaticRule(Priority = 969)]
public sealed class NontokenCreaturesHaveKeywordRule : IStaticRule
{
  // "Nontoken creatures you control have <keyword>." with optional reminder text.
  // Anchored (^ ... $) to prevent substring matches.
  private static readonly Regex _pattern = new(
    @"^\s*Nontoken\s+creatures\s+you\s+control\s+have\s+(?<kw>[a-z][a-z ]+?)\.?\s*(?<reminder>\([^)]+\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Match against the full clause raw text (including any trailing reminder).
    // The pattern's <reminder> group captures the parenthetical if present.
    var m = _pattern.Match(clause.RawText);
    if (!m.Success)
    {
      return null;
    }

    var kw = m.Groups["kw"].Value.Trim().ToLowerInvariant();
    var grantedAbility = StaticRuleHelpers.MapKeywordToStaticAbility(kw);
    if (grantedAbility is null)
    {
      return null;
    }

    // Preserve reminder text when the parenthetical is present.
    var reminderRaw = m.Groups["reminder"].Value.Trim();
    Parenthetical? reminder = string.IsNullOrEmpty(reminderRaw)
      ? null
      : new Parenthetical { Text = reminderRaw };

    return
    [
      new StaticAbility
      {
        Reminder = reminder,
        Effects = [new GainAbilityEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              IsToken = false,
              Controller = ControllerFilter.You,
            },
          },
          GainedAbility = grantedAbility,
        }],
      },
    ];
  }
}
