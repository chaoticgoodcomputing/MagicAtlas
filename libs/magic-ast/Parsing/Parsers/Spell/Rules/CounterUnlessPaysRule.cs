namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Counter target spell unless its controller pays {COST}."
/// and typed variants:
///   - "Counter target noncreature spell unless its controller pays {2}." (Spell Pierce)
///   - "Counter target creature spell unless its controller pays {2}."
///   - "Counter target spell unless its controller pays {3}." (Mana Leak)
///
/// Pattern: "Counter target [TYPE] spell unless its controller pays {COST}."
/// where TYPE is an optional spell-subset qualifier (creature, noncreature, instant,
/// sorcery) and COST is any mana expression ({1}, {X}, {2}{U}, etc.).
///
/// Priority 60: fires before <see cref="CounterSpellRule"/> (priority 50) so the
/// unless-pays shape is owned by this rule. Rule 701.6: to counter a spell, it's put
/// into its owner's graveyard; the unless clause (Rule 117.7) gives the target's
/// controller an opportunity to pay a cost to prevent the counterspell from happening.
/// </summary>
[SpellRule(Priority = 60)]
public sealed class CounterUnlessPaysRule : ISpellRule
{
  // Captures:
  //   <cardtype>  — optional spell-subset qualifier (creature, noncreature, instant, sorcery)
  //   <unless>    — the mana expression the controller may pay ({1}, {X}, {2}{U}, …)
  private static readonly Regex Pattern = new(
    @"^Counter\s+target\s+"
    + @"(?<cardtype>creature|noncreature|instant|sorcery)?\s*"
    + @"spell\s+unless\s+its\s+controller\s+pays\s+(?<unless>(?:\{[^}]+\})+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private readonly ManaCostParser _manaCostParser = new();

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var cardTypeWord = m.Groups["cardtype"].Success ? m.Groups["cardtype"].Value : null;
    var filter = SpellRuleHelpers.BuildSpellFilter([], cardTypeWord: cardTypeWord);

    var parsed = _manaCostParser.Parse(m.Groups["unless"].Value);
    var unless = new UnlessClause
    {
      Player = new ObjectReference { Kind = ObjectReferenceKind.Controller },
      Cost = new ManaCost { Symbols = [.. parsed.Symbols] },
    };

    effect = new CounterSpellEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.Target, Filter = filter },
      UnlessClause = unless,
    };
    return true;
  }
}
