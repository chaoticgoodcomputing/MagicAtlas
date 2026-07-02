namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target player discards [a/N] card[s]." and
/// "Target opponent discards [a/N] card[s]."
///
/// <para>Covered oracle shapes:</para>
/// <list type="bullet">
///   <item>"Target player discards a card." (Raven's Crime, Blackmail)</item>
///   <item>"Target player discards two cards." (Mind Rot)</item>
///   <item>"Target opponent discards a card." (Duress variants)</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class DiscardTargetPlayerRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Target\s+(?<subject>player|opponent)\s+discards?\s+(?<count>[a-z]+|\d+)\s+cards?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var raw = m.Groups["count"].Value;
    if (!SpellRuleHelpers.TryParseSmallWord(raw, out var n))
    {
      return false;
    }

    var isOpponent = m.Groups["subject"].Value.Equals("opponent", StringComparison.OrdinalIgnoreCase);

    var player = isOpponent
      ? new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["opponent"] },
        }
      : new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["player"] },
        };

    effect = new DiscardCardsEffect
    {
      Count = LiteralQuantity.Of(n),
      Player = player,
      Random = false,
    };
    return true;
  }
}
