namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the mass P/T-modification shape for creature subsets:
///   "Creatures you control get +N/+M until end of turn."
///   "Creatures you control get -N/-M until end of turn."
///   "Creatures your opponents control get -N/-M until end of turn."
///   "All creatures get +N/+M until end of turn."
///   "All creatures get -N/-M until end of turn."
///   "Attacking creatures get +N/+M until end of turn."
///   "Blocking creatures get +N/+M until end of turn."
///
/// This is the mass anthem/debuff version. The single-target
/// "Target creature gets +N/+M ..." shape is handled separately
/// by <see cref="ModifyPTSpellRule"/>.
///
/// Examples:
/// <list type="bullet">
///   <item>"Creatures you control get +1/+1 until end of turn."             (Charge)</item>
///   <item>"Creatures you control get +0/+4 until end of turn."             (Bar the Door)</item>
///   <item>"All creatures get -1/-1 until end of turn."                     (Shrivel)</item>
///   <item>"All creatures get -2/-2 until end of turn."                     (Infest)</item>
///   <item>"All creatures get -4/-4 until end of turn."                     (Languish)</item>
///   <item>"Attacking creatures get +2/+0 until end of turn."               (Army of Allah)</item>
///   <item>"Blocking creatures get +0/+3 until end of turn."                (Piety)</item>
///   <item>"Creatures your opponents control get -1/-1 until end of turn."  (Cower in Fear)</item>
///   <item>"Creatures your opponents control get -1/-1 until end of turn."  (Make Obsolete)</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class MassAnthemSpellRule : ISpellRule
{
  // Named capture group <subj> selects the subject phrase.
  // Subjects handled:
  //   "All creatures"                        → Each, all creatures, no controller filter
  //   "Creatures you control"                → Each, creature, Controller=You
  //   "Creatures your opponents control"     → Each, creature, Controller=Opponent
  //   "Attacking creatures"                  → Each, creature, Characteristics=[Characteristic.Other("attacking")]
  //   "Blocking creatures"                   → Each, creature, Characteristics=[Characteristic.Other("blocking")]
  private static readonly Regex _pattern = new(
    @"^(?<subj>All\s+creatures|Creatures\s+you\s+control|Creatures\s+your\s+opponents\s+control|Attacking\s+creatures|Blocking\s+creatures)"
    + @"\s+get\s+(?<p>[+\-]\d+)/(?<t>[+\-]\d+)\s+until\s+end\s+of\s+turn$",
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

    var power = int.Parse(m.Groups["p"].Value);
    var toughness = int.Parse(m.Groups["t"].Value);
    var subj = m.Groups["subj"].Value;

    var filter = BuildFilter(subj);

    effect = new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = filter,
      },
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = new UntilEndOfTurnDuration(),
    };
    return true;
  }

  private static ObjectFilter BuildFilter(string subj)
  {
    // Normalise whitespace for comparison.
    var s = subj.Trim();

    if (s.Equals("Creatures you control", StringComparison.OrdinalIgnoreCase))
    {
      return new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = ControllerFilter.You,
      };
    }

    if (s.Equals("Creatures your opponents control", StringComparison.OrdinalIgnoreCase))
    {
      return new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = ControllerFilter.Opponent,
      };
    }

    if (s.Equals("Attacking creatures", StringComparison.OrdinalIgnoreCase))
    {
      return new ObjectFilter
      {
        CardTypes = ["creature"],
        Characteristics = [Characteristic.Other("attacking")],
      };
    }

    if (s.Equals("Blocking creatures", StringComparison.OrdinalIgnoreCase))
    {
      return new ObjectFilter
      {
        CardTypes = ["creature"],
        Characteristics = [Characteristic.Other("blocking")],
      };
    }

    // "All creatures" — no controller restriction.
    return new ObjectFilter
    {
      CardTypes = ["creature"],
    };
  }
}
