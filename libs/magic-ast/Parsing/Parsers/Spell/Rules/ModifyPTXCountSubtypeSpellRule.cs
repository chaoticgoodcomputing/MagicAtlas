namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the P/T-modification shape whose X is DEFINED by the spell as an
/// object count, rather than announced by the caster:
///   "Target creature gets +X/+X until end of turn, where X is the number of
///    Elves on the battlefield."  (Wirewood Pride)
///
/// <para>
/// CR 107.3: "Many objects use the letter X as a placeholder for a number that
/// needs to be determined. Some objects have abilities that define the value of X …"
/// Here the ability defines X ("where X is the number of Elves on the battlefield"),
/// so both P/T modifiers resolve to the SAME game-state count rather than a caster
/// -announced <see cref="VariableQuantity"/>. Reference-not-resolution (ADR 0004):
/// the count is modelled structurally as a <see cref="CountQuantity"/> over an
/// <see cref="ObjectFilter"/> (the creature subtype in the battlefield zone); the
/// engine evaluates the actual number at resolution time.
/// </para>
///
/// <para>
/// CR 613.4 (Layer 7 sublayers): a "+X/+X" modifier that adds to power/toughness
/// (rather than setting them) is a P/T-modifying effect, hence
/// <see cref="ModifyPTEffect"/> (not a set-P/T effect). "on the battlefield" carries
/// no controller restriction, so the filter has <see cref="Zone.Battlefield"/> with
/// no <see cref="ObjectFilter.Controller"/> — every Elf, regardless of controller.
/// </para>
///
/// Anchored (<c>^…$</c>) and distinct from <see cref="ModifyPTBothVariableSpellRule"/>,
/// whose pattern ends at "until end of turn" with no ", where X is …" tail.
/// </summary>
[SpellRule(Priority = 60)]
public sealed class ModifyPTXCountSubtypeSpellRule : ISpellRule
{
  // "Target creature gets +X/+X until end of turn, where X is the number of
  //  <noun> on the battlefield" — noun is a plural creature subtype (e.g. Elves).
  private static readonly Regex _pattern = new(
    @"^Target\s+creature\s+gets\s+\+X/\+X\s+until\s+end\s+of\s+turn,\s+where\s+X\s+is\s+the\s+number\s+of\s+(?<noun>[A-Za-z]+)\s+on\s+the\s+battlefield$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var subtype = Singularize(m.Groups["noun"].Value);

    Quantity Count() =>
      new CountQuantity
      {
        CountOf = new ObjectFilter { Subtypes = [subtype], Zone = Zone.Battlefield },
      };

    effect = new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      PowerModifier = Count(),
      ToughnessModifier = Count(),
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }

  /// <summary>
  /// Singularizes a plural creature-subtype noun and title-cases it (subtype names
  /// are proper-noun-capitalised per CR 205.3m). Handles the "-ves" irregular
  /// (Elves→Elf, Wolves→Wolf, Dwarves→Dwarf) and the regular trailing "-s".
  /// </summary>
  private static string Singularize(string plural)
  {
    var s = plural;
    if (s.EndsWith("ves", StringComparison.OrdinalIgnoreCase))
    {
      s = s[..^3] + "f";
    }
    else if (s.EndsWith("s", StringComparison.OrdinalIgnoreCase))
    {
      s = s[..^1];
    }
    if (s.Length > 0)
    {
      s = char.ToUpperInvariant(s[0]) + s[1..];
    }
    return s;
  }
}
