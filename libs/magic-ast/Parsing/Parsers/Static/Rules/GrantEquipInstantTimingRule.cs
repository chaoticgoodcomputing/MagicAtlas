namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "You may activate equip abilities any time you could cast an instant." —
/// Leonin Shikari. Grants instant-speed timing (CR 601/602 activation timing) to
/// a referenced class of abilities. The affected class is a typed
/// <see cref="ActivatedAbilityReference"/> keyed on the surviving
/// <see cref="KeywordAbility.Equip"/> identity (ADR 0003): the keyword that
/// survives decomposition is the same identity this timing grant filters on.
///
/// <para>
/// Shares the <see cref="AbilityReference"/> value type with
/// <see cref="AppliesToCostReductionRule"/>; here it is carried on
/// <see cref="TimingModificationEffect.AppliesTo"/> rather than a cost effect.
/// </para>
/// </summary>
[StaticRule(Priority = 990)]
public sealed class GrantEquipInstantTimingRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+activate\s+equip\s+abilities\s+any\s+time\s+you\s+could\s+cast\s+an\s+instant\.?\s*$",
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
        Effects = [new TimingModificationEffect
        {
          Modification = TimingModificationType.Grant,
          Timing = TimingWindow.Instant,
          AppliesTo = new ActivatedAbilityReference
          {
            Keyword = KeywordAbility.Equip,
            Controller = ControllerFilter.You,
          },
        }],
      },
    ];
  }
}
