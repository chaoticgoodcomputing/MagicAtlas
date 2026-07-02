namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "return target instant or sorcery card from your graveyard to your hand"
///
/// Models the Izzet Chronarch / Archaeomancer family (Rule 701.9: Return).
/// The oracle text always targets a card by type-disjunction ("instant or sorcery")
/// in the controller's graveyard and moves it to the controller's hand.
///
/// Zone = Graveyard encodes the source zone stated in oracle text;
/// Controller = You encodes "your graveyard".
/// CardTypes = ["instant", "sorcery"] encodes the disjunction (Rule 700.4).
///
/// Priorities above the generic ReturnToHandRule (default 50) because this rule
/// is more specific (graveyard source + type-disjunction shape).
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class ReturnInstantOrSorceryFromGraveyardRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^return\s+target\s+instant\s+or\s+sorcery\s+card\s+from\s+your\s+graveyard\s+to\s+your\s+hand$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim().TrimEnd('.')))
    {
      return false;
    }

    effect = new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["instant", "sorcery"],
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
    };
    return true;
  }
}
