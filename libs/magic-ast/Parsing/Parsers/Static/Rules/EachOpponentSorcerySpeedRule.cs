namespace MagicAST.Parsing.Parsers.Static.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Each opponent can cast spells only any time they could cast a sorcery." —
/// Teferi, Time Raveler and similar effects that restrict opponents to casting
/// spells only at sorcery speed. This is a continuous static effect (CR 604.2 /
/// CR 611.1) that modifies the rules of the game for all opponents: they may
/// only cast spells during their own main phase when the stack is empty, rather
/// than at instant speed.
///
/// <para>
/// CR 116.1b: "A player can cast an instant spell any time they have priority.
/// A player can cast a noninstant spell during their main phase when the stack
/// is empty." This restriction locks opponents to the latter (sorcery-speed)
/// window, preventing instant-speed interaction.
/// </para>
///
/// <para>
/// Modelled as a <see cref="TimingModificationEffect"/> with
/// <c>Modification = Restrict</c> and <c>Timing = Sorcery</c>, where
/// <c>AppliesTo</c> is a <see cref="SpellReference"/> constraining opponents'
/// spells (<c>Controller = Opponent</c>).
/// </para>
/// </summary>
[StaticRule(Priority = 980)]
public sealed class EachOpponentSorcerySpeedRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Each\s+opponent\s+can\s+cast\s+spells\s+only\s+any\s+time\s+they\s+could\s+cast\s+a\s+sorcery\.?\s*$",
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
          new TimingModificationEffect
          {
            Modification = TimingModificationType.Restrict,
            Timing = TimingWindow.Sorcery,
            AppliesTo = new SpellReference
            {
              Filter = new ObjectFilter
              {
                Controller = ControllerFilter.Opponent,
              },
            },
          },
        ],
      },
    ];
  }
}
