namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you lose N life if you [don't ]control a [filter]" — a resolution-time
/// conditional life-loss effect (Takenuma Bleeder: "Whenever this creature attacks
/// or blocks, you lose 1 life if you don't control a Demon."). The trigger half is
/// handled upstream (AttacksOrBlocksConditionRule); this rule structures the effect
/// interior, which otherwise falls through to an <see cref="UnstructuredEffect"/>
/// shell (fidelity L1).
///
/// <para>
/// The "if" here does NOT immediately follow the trigger condition (it follows the
/// effect verb "you lose 1 life"), so by CR 603.4 it is NOT an intervening-'if'
/// clause — it "has only its normal English meaning": a within-resolution gate.
/// It is therefore modelled as a <see cref="ConditionalEffect"/> inside the trigger's
/// Effects list, NOT as the trigger's InterveningIf. The condition is a
/// <see cref="CountCondition"/> composing <see cref="ObjectFilter"/> +
/// <see cref="Comparison"/> (ADR 0007), exactly as Gravecrawler encodes "you control
/// a Zombie" — negation ("don't control a") is the same count compared
/// <see cref="ComparisonOperator.LessThan"/> 1 (i.e. you control zero of them).
/// The gated effect is a plain <see cref="LoseLifeEffect"/> (reference-not-resolution,
/// ADR 0004: MAST records the printed if-then; the engine evaluates it).
/// </para>
///
/// <para>
/// CR 603.4 (verbatim, in part): "The word 'if' has only its normal English meaning
/// anywhere else in the text of a card; this rule only applies to an 'if' that
/// immediately follows a trigger condition."
/// CR 119.3 (verbatim): "If an effect causes a player to gain life or lose life,
/// that player's life total is adjusted accordingly."
/// </para>
///
/// Anchored (^…$) on the full effect fragment, so it cannot match a substring of a
/// longer clause or shadow a sibling shape.
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class YouLoseLifeIfYouControlFilterRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^you\s+lose\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life\s+if\s+you\s+(?<neg>don't\s+)?control\s+(?:a|an)\s+(?<noun>[A-Za-z][A-Za-z\- ]*?)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlySet<string> CardTypeNouns = new HashSet<string>(
    StringComparer.OrdinalIgnoreCase)
  {
    "card", "creature", "artifact", "enchantment", "land", "planeswalker",
    "instant", "sorcery", "permanent", "spell", "token", "battle",
  };

  private static readonly IReadOnlyDictionary<string, int> NumberWords =
    new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
      ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
      ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10,
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var amount = NumberWords.TryGetValue(m.Groups["amount"].Value, out var n)
      ? n
      : int.Parse(m.Groups["amount"].Value);

    var negated = m.Groups["neg"].Success;
    var filter = NounToFilter(m.Groups["noun"].Value.Trim()) with
    {
      Controller = ControllerFilter.You,
    };

    // "control a X" → count ≥ 1; "don't control a X" → count < 1 (zero).
    var comparison = negated
      ? new Comparison { Operator = ComparisonOperator.LessThan, Value = 1 }
      : new Comparison { Operator = ComparisonOperator.GreaterThanOrEqual, Value = 1 };

    effect = new ConditionalEffect
    {
      Condition = new CountCondition { Filter = filter, Count = comparison },
      Then = new LoseLifeEffect
      {
        Amount = LiteralQuantity.Of(amount),
        Player = ObjectReference.You(),
      },
    };
    return true;
  }

  /// <summary>
  /// "Demon" → Subtypes:["Demon"]; a card-type noun ("creature", "artifact") →
  /// CardTypes (lowercase singular). Mirrors the ConditionParser noun-classification
  /// convention without editing that shared file.
  /// </summary>
  private static ObjectFilter NounToFilter(string noun)
  {
    var singular = noun.EndsWith("s", StringComparison.Ordinal) ? noun[..^1] : noun;
    return CardTypeNouns.Contains(singular)
      ? new ObjectFilter { CardTypes = [singular.ToLowerInvariant()] }
      : new ObjectFilter { Subtypes = [char.ToUpperInvariant(singular[0]) + singular[1..]] };
  }
}
