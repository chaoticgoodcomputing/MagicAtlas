namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;

/// <summary>
/// "As this creature enters, choose a nonland card name." (Silverquill Silencer)
/// — the NONLAND-restricted sibling of <see cref="ChooseCardNameOnEntryRule"/>'s
/// unrestricted "choose a card name" (Declaration of Naught). Recognizes the
/// as-enters card-name-choice declaration and emits a composite
/// <see cref="StaticAbility"/> carrying <see cref="StaticTimingKind.AsThisEnters"/>
/// (CR 614.12) plus a <see cref="ChooseCardNameEffect"/> whose
/// <see cref="ChooseCardNameEffect.Filter"/> narrows the choosable name to
/// nonland cards (<c>ObjectFilter{CardTypes:["card"], ExcludedCardTypes:["land"]}</c>
/// — the same "nonland" encoding used elsewhere, e.g. the Thoughtseize family's
/// "you choose a nonland card").
///
/// <para>
/// Anchored end-to-end and requires the literal "nonland" token between "choose
/// a" and "card name", so this rule is naturally disjoint from
/// <see cref="ChooseCardNameOnEntryRule"/>'s pattern (which has no "nonland"
/// token and therefore never matches this card's text) — dispatch priority
/// relative to that rule is immaterial.
/// </para>
/// </summary>
[StaticRule(Priority = 943)]
public sealed class ChooseNonlandCardNameOnEntryRule : IStaticRule
{
  private static readonly Regex _chooseNonlandCardNameOnEntryPattern = new(
    @"^\s*As\s+this\s+(?:permanent|land|creature|artifact|enchantment)\s+enters,\s+choose\s+a\s+nonland\s+card\s+name\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _chooseNonlandCardNameOnEntryPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        When = StaticTimingKind.AsThisEnters,
        Effects =
        [
          new ChooseCardNameEffect
          {
            Filter = new ObjectFilter
            {
              CardTypes = ["card"],
              ExcludedCardTypes = ["land"],
            },
          },
        ],
      },
    ];
  }
}
