namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// "Counter target [Q1] or [Q2] spell." — Nullify ("creature or Aura"), Hisoka's
/// Defiance ("Spirit or Arcane"). Token type classified by vocabulary, not position.
/// Priority 80: overrides <see cref="CounterSpellRule"/> whose color-disjunction
/// branch could otherwise capture two-token disjunctions.
/// </summary>
[SpellRule(Priority = 80)]
public sealed class CounterTargetTypeOrSubtypeSpellRule : ISpellRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Regex.Match(
      text,
      @"^Counter\s+target\s+(?<q1>[A-Za-z]+)\s+or\s+(?<q2>[A-Za-z]+)\s+spell$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return false;
    }

    var colorWords = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
      "white", "blue", "black", "red", "green", "colorless", "multicolored",
    };
    if (colorWords.Contains(m.Groups["q1"].Value) || colorWords.Contains(m.Groups["q2"].Value))
    {
      return false;
    }

    var cardTypeVocab = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
      "creature", "artifact", "enchantment", "land", "planeswalker",
      "instant", "sorcery", "tribal", "battle",
    };

    var cardTypes = new List<string> { "spell" };
    var subtypes = new List<string>();
    foreach (var q in new[] { m.Groups["q1"].Value, m.Groups["q2"].Value })
    {
      if (cardTypeVocab.Contains(q))
      {
        cardTypes.Add(q.ToLowerInvariant());
      }
      else
      {
        subtypes.Add(q);
      }
    }

    effect = new CounterSpellEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = cardTypes,
          Subtypes = subtypes.Count > 0 ? subtypes : null,
        },
      },
    };
    return true;
  }
}
