namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "You may cast spells as though they had flash." — Vedalken Orrery and similar
/// global flash-grant static abilities. This is a continuous static permission
/// (CR 604.2) that allows the controlling player to cast any spell at instant
/// speed, modelled as a <see cref="TimingModificationEffect"/> whose
/// <see cref="TimingModificationEffect.AppliesTo"/> targets all spells you cast.
///
/// <para>
/// CR 702.8a: "Flash is a static ability that functions in any zone from which
/// you could play the card it's on. 'Flash' means 'You may play this card any
/// time you could cast an instant.'" The global grant here extends that
/// permission to every spell the controller casts rather than to a single card.
/// </para>
/// </summary>
[StaticRule(Priority = 990)]
public sealed class CastSpellsAsFlashRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+cast\s+spells\s+as\s+though\s+they\s+had\s+flash\.?\s*$",
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
                Controller = ControllerFilter.You,
              },
            },
          },
        ],
      },
    ];
  }
}
