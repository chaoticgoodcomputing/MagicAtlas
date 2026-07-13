namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "Your opponents can't cast spells during your turn." (Dragonlord Dromoka) — a
/// continuous static cast prohibition (CR 601.3a: a spell can't be cast if doing
/// so would violate a rule or effect that says a player can't cast a spell) scoped
/// two ways at once: WHO is prohibited (a player class — "your opponents") and
/// WHEN the prohibition is in effect (a timing window — "during your turn").
///
/// <para>
/// Reuses <see cref="CantBeCastEffect"/> (the existing cast-prohibition node,
/// already established by <c>CantBeCastRestrictionRule</c> for the mana-value and
/// {X}-in-cost shapes on Gaddock Teeg) rather than inventing a new effect: the
/// WHO half is expressed the same way those siblings express "which spells" —
/// via the enclosing <see cref="StaticAbility.AffectedObjects"/> filter, here
/// scoped by <see cref="ObjectFilter.Controller"/> = <see cref="ControllerFilter.Opponent"/>
/// (a spell's controller is the player casting it, CR 108.4a) rather than by card
/// type/mana-value characteristics.
/// </para>
///
/// <para>
/// The WHEN half reuses the established "during your turn" timing qualifier —
/// an <see cref="OtherCondition"/> carrying the verbatim phrase "During your turn"
/// (the same PB-7 structured-condition bucket <c>SelfKeywordDuringTurnSuffixRule</c>
/// uses for Razorkin Needlehead's "This creature has first strike during your
/// turn.") on <see cref="StaticAbility.Condition"/>, produced via the shared
/// <see cref="MagicAST.Parsing.ConditionParser"/> entry point rather than hand-built.
/// </para>
///
/// Anchored (^…$) so the subject phrase "Your opponents can't cast spells" cannot
/// match as a substring of a more specific sibling static line.
/// </summary>
[StaticRule(Priority = 971)]
public sealed class OpponentsCantCastDuringYourTurnRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Your\s+opponents\s+can'?t\s+cast\s+spells\s+during\s+your\s+turn\.?\s*$",
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
        Effects = [new CantBeCastEffect()],
        AffectedObjects = new ObjectFilter
        {
          CardTypes = ["spell"],
          Controller = ControllerFilter.Opponent,
        },
        Condition = MagicAST.Parsing.ConditionParser.Parse("During your turn"),
      },
    ];
  }
}
