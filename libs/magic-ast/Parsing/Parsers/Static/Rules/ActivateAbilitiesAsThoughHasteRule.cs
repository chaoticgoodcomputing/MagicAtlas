namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "You may activate abilities of creatures you control as though those
/// creatures had haste." — Thousand-Year Elixir.
///
/// <para>
/// CR 302.6: a creature's activated ability with {T}/{Q} can't be activated
/// unless it's been under control continuously since your most recent turn
/// began. CR 702.10a: "Haste is a static ability." This static permission
/// removes the CR 302.6 activation restriction for the named creatures'
/// activated abilities without granting them haste outright (see
/// <see cref="ActivateAbilitiesAsThoughHasteEffect"/>).
/// </para>
///
/// <para>
/// Anchored start-to-end (^…$) so this only claims the exact singular
/// permission sentence and cannot swallow a longer sibling clause that
/// appends a rider.
/// </para>
/// </summary>
[StaticRule(Priority = 990)]
public sealed class ActivateAbilitiesAsThoughHasteRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+activate\s+abilities\s+of\s+creatures\s+you\s+control\s+as\s+though\s+those\s+creatures\s+had\s+haste\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new ActivateAbilitiesAsThoughHasteEffect
          {
            AppliesTo = new ObjectActivatedAbilityReference
            {
              PermanentFilter = new ObjectFilter
              {
                CardTypes = ["creature"],
                Controller = ControllerFilter.You,
              },
            },
          },
        ],
      },
    ];
  }
}
