namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;

/// <summary>
/// "As this [permanent] enters, choose [Name] or [Name]." (Phenomenon Investigators —
/// "As this creature enters, choose Believe or Doubt.") — the as-enters named-mode
/// binder of the Believe/Doubt family. Emits a composite <see cref="StaticAbility"/>
/// carrying <see cref="StaticTimingKind.AsThisEnters"/> (CR 614.12) plus a plain
/// <see cref="ChooseNamedOptionEffect"/> listing the printed option names in order.
///
/// <para>
/// Sibling of <see cref="ChoosePlayerOnEntryRule"/> / <see cref="ChooseColorOnEntryRule"/> /
/// <see cref="ChooseBasicLandTypeOnEntryRule"/>: those bind a choice from a characteristic
/// domain ("choose a player/color", "choose Island or Swamp"), this binds a choice among a
/// closed set of card-specific NAMED modes ("choose Believe or Doubt"). The lowercase
/// "a player"/"a color" forms never match this rule's two-Capitalised-word regex; the
/// basic-land-type form ("choose Island or Swamp") and the five colour words WOULD match on
/// surface, so they are explicitly excluded here (<see cref="_reservedWords"/>) and deferred
/// to their dedicated sibling rules — mode names are, by construction, none of the printed
/// characteristic vocabularies. The two ability lines that follow (each labelled with an
/// option name) are handled by <see cref="NamedModeGatedAbilityRule"/> and carry a
/// <see cref="ChosenModeCondition"/> gate.
/// </para>
/// </summary>
[StaticRule(Priority = 1003)]
public sealed class ChooseNamedOptionOnEntryRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*As\s+this\s+(?:permanent|creature|land|artifact|enchantment|planeswalker)\s+enters,\s+choose\s+(?<a>[A-Z][A-Za-z]+)\s+or\s+(?<b>[A-Z][A-Za-z]+)\.?\s*$",
    RegexOptions.Compiled
  );

  // Printed characteristic vocabularies with their own dedicated choose-on-entry rules
  // (basic land types — CR 305.6; colours — CR 105.1). A named MODE is never one of these,
  // so if either option is reserved this is a characteristic choice, not a named-mode binder,
  // and must defer to the sibling rule regardless of dispatch order.
  private static readonly HashSet<string> _reservedWords = new(StringComparer.Ordinal)
  {
    "Plains", "Island", "Swamp", "Mountain", "Forest",
    "White", "Blue", "Black", "Red", "Green",
  };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    if (_reservedWords.Contains(match.Groups["a"].Value) || _reservedWords.Contains(match.Groups["b"].Value))
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
          new ChooseNamedOptionEffect
          {
            Options = [match.Groups["a"].Value, match.Groups["b"].Value],
          },
        ],
      },
    ];
  }
}
