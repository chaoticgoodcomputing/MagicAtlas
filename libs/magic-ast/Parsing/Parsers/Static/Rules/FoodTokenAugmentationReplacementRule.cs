namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.References;

/// <summary>
/// Token-augmentation replacement effect (Peregrin Took family):
/// "If one or more tokens would be created under your control, those tokens plus
/// an additional Food token are created instead."
///
/// CR 614.1: A replacement effect watches for a particular event — "If [event]
/// would happen, [modified result] instead" — and modifies that event. The original
/// tokens are still created (<see cref="ReplacementEffect.OriginalEventOccurs"/> is
/// true), and one additional Food token is created alongside them.
///
/// Distinct from <see cref="TokenAugmentationReplacementRule"/> (Chatterfang family),
/// which adds a creature token matching the count of the original tokens ("that many"),
/// and from <see cref="TokenDoublingReplacementRule"/> (Doubling Season family), which
/// doubles the quantity. This family adds a single named token subtype regardless of
/// the original token count.
///
/// The trailing parenthetical "(It's an artifact with ...)" is reminder text for the
/// Food token (CR 207.2) and is preserved on <see cref="Ability.Reminder"/>.
/// </summary>
[StaticRule(Priority = 977)]
public sealed class FoodTokenAugmentationReplacementRule : IStaticRule
{
  // Pattern for the Food-augmentation shape:
  //   "If one or more tokens would be created under your control,
  //    those tokens plus an additional Food token are created instead."
  // The controller scope "under your control" is fixed on this pattern — this
  // is a self-benefit replacement (CR 614.1b: applying to events "under your
  // control"), not a universal one.
  private static readonly Regex _pattern = new(
    @"^\s*If\s+one\s+or\s+more\s+tokens\s+would\s+be\s+created\s+under\s+your\s+control,\s+those\s+tokens\s+plus\s+an\s+additional\s+Food\s+token\s+are\s+created\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Captures the inner text of a trailing reminder parenthetical (CR 207.2).
  private static readonly Regex _reminderPattern = new(
    @"\(\s*(?<reminder>[^)]*?)\s*\)\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var body = StaticRuleHelpers.StripReminderText(clause.RawText);
    if (!_pattern.IsMatch(body))
    {
      return null;
    }

    Parenthetical? reminder = null;
    var reminderMatch = _reminderPattern.Match(clause.RawText);
    if (reminderMatch.Success)
    {
      reminder = new Parenthetical { Text = reminderMatch.Groups["reminder"].Value };
    }

    return
    [
      new StaticAbility
      {
        Reminder = reminder,
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.TokenCreationEvent
          {
            MinimumQuantity = 1,
            Controller = ObjectReference.You(),
          },
          OriginalEventOccurs = true,
          Replacement = new MagicAST.AST.Effects.TokenCopy.CreateTokenEffect
          {
            Player = ObjectReference.You(),
            Count = MagicAST.AST.Quantities.LiteralQuantity.Of(1),
            Token = TokenDefinition.Food(),
          },
        }],
      },
    ];
  }
}
