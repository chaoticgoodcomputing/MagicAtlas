namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Recognises the Necrobloom static paragraph:
/// "Land cards in your graveyard have dredge N. ([reminder])"
///
/// <para>
/// This is a continuous static ability (CR 604) that grants Dredge N (CR 702.52)
/// to every land card in the controller's graveyard. The parenthetical is standard
/// Dredge reminder text (CR 207.2) and is captured into <see cref="Ability.Reminder"/>.
/// </para>
///
/// <para>
/// The granted ability is modelled as a <see cref="GainAbilityEffect"/> whose
/// <see cref="GainAbilityEffect.Target"/> is each land card in your graveyard
/// (Filter: CardTypes=["land"], Controller=You, Zone=Graveyard) and whose
/// <see cref="GainAbilityEffect.GainedAbility"/> is a <see cref="StaticAbility"/>
/// with <see cref="DredgeEffect"/> and <see cref="StaticAbility.KeywordSource"/> = "Dredge".
/// Mirrors <see cref="EachNonlandGraveyardHasEscapeRule"/> (Underworld Breach) over the
/// land-type axis and with Dredge in place of Escape.
/// </para>
///
/// <para>
/// Rule citations: CR 604 (static abilities), CR 702.52 (Dredge),
/// CR 207.2 (reminder text), CR 406.3 (graveyard zone).
/// </para>
/// </summary>
[StaticRule(Priority = 50)]
public sealed class LandCardsInGraveyardHaveDredgeRule : IStaticRule
{
  // Matches: "Land cards in your graveyard have dredge N." with optional reminder.
  // Named group "n" captures the dredge value.
  // Named group "reminder" captures the optional trailing parenthetical.
  private static readonly Regex _pattern = new(
    @"^\s*Land\s+cards\s+in\s+your\s+graveyard\s+have\s+dredge\s+(?<n>\d+)\."
      + @"\s*(?<reminder>\([^)]*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    if (!int.TryParse(match.Groups["n"].Value, out var dredgeValue))
    {
      return null;
    }

    // Capture the optional reminder parenthetical if present.
    Parenthetical? reminder = null;
    var reminderGroup = match.Groups["reminder"];
    if (reminderGroup.Success && reminderGroup.Value.Length > 0)
    {
      reminder = new Parenthetical { Text = reminderGroup.Value };
    }

    // The granted dredge ability: DredgeEffect with the printed N value.
    var grantedDredgeAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Dredge,
      Effects =
      [
        new DredgeEffect
        {
          Value = dredgeValue,
        },
      ],
    };

    // The outer static ability grants dredge to each land card in your graveyard.
    // Filter: CardTypes=["land"], Controller=You, Zone=Graveyard.
    return
    [
      new StaticAbility
      {
        Reminder = reminder,
        Effects =
        [
          new GainAbilityEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["land"],
                Controller = ControllerFilter.You,
                Zone = Zone.Graveyard,
              },
            },
            GainedAbility = grantedDredgeAbility,
          },
        ],
      },
    ];
  }
}
