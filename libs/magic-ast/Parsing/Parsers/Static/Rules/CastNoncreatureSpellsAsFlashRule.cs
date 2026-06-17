namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "You may cast noncreature spells as though they had flash." — Valley Floodcaller
/// and similar cards that grant flash specifically to noncreature spells. This is a
/// continuous static permission (CR 604.2) that allows the controlling player to cast
/// noncreature spells at instant speed, modelled as a
/// <see cref="TimingModificationEffect"/> whose <see cref="TimingModificationEffect.AppliesTo"/>
/// targets noncreature spells the controller casts.
///
/// <para>
/// CR 702.8a: "Flash is a static ability that functions in any zone from which you
/// could play the card it's on. 'Flash' means 'You may play this card any time you
/// could cast an instant.'" The global grant here extends that permission to every
/// noncreature spell the controller casts. The filter encodes the noncreature
/// restriction via <c>ExcludedCardTypes = ["creature"]</c> on the
/// <see cref="SpellReference"/> (parallel to the
/// <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.SpellCastConditionRule"/>
/// "noncreature spell" encoding).
/// </para>
/// </summary>
[StaticRule(Priority = 991)]
public sealed class CastNoncreatureSpellsAsFlashRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+cast\s+noncreature\s+spells\s+as\s+though\s+they\s+had\s+flash\.?\s*$",
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
            Modification = TimingModificationType.Grant,
            Timing = TimingWindow.Instant,
            AppliesTo = new SpellReference
            {
              Filter = new ObjectFilter
              {
                CardTypes = ["spell"],
                ExcludedCardTypes = ["creature"],
                Controller = ControllerFilter.You,
              },
            },
          },
        ],
      },
    ];
  }
}
