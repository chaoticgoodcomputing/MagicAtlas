namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "You may play lands from your graveyard." — a static permission granting the
/// controller the ability to play land cards from their graveyard.
///
/// <para>
/// CR 305.1: "A player who has priority may play a land card from their hand
/// during a main phase of their turn when the stack is empty. Playing a land is
/// a special action; it doesn't use the stack." This rule extends that
/// permission so eligible land cards may come from the graveyard. The
/// description is static (it persists as long as the source is on the
/// battlefield — CR 604.2); MAST models the permission, not the execution.
/// Crucible of Worlds and Ramunap Excavator share this exact oracle line.
/// </para>
/// </summary>
[StaticRule]
public sealed class MayPlayLandsFromGraveyardRule : IStaticRule
{
  // "You may play lands from your graveyard."
  private static readonly Regex Pattern = new(
    @"^\s*You\s+may\s+play\s+lands\s+from\s+your\s+graveyard\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!Pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MayPlayFromGraveyardEffect
          {
            Cards = new ObjectFilter
            {
              CardTypes = ["land"],
              Zone = Zone.Graveyard,
              Controller = ControllerFilter.You,
            },
          },
        ],
      },
    ];
  }
}
