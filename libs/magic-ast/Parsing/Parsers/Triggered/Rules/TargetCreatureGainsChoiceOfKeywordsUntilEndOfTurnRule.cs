namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Static;

/// <summary>
/// "target creature gains your choice of &lt;kw1&gt; or &lt;kw2&gt;[ or …] until
/// end of turn." — a triggered-ability effect clause that grants the target
/// creature one of several keyword abilities, the controller's choice at
/// resolution (Éowyn, Lady of Rohan: "…target creature gains your choice of
/// first strike or vigilance until end of turn.").
///
/// <para>
/// The "your choice of X or Y" is modelled as a <see cref="ModalEffect"/> with
/// <see cref="ModeSelection.ChooseOne"/> and one <see cref="ModalOption"/> per
/// keyword — the same modal primitive used for "create a Food token or a
/// Treasure token" (Tireless Provisioner). Each option is a
/// <see cref="SpellAbility"/> whose single <see cref="GainAbilityEffect"/> grants
/// the keyword to the chosen target creature (<see cref="ObjectReferenceKind.Target"/>)
/// with an "until end of turn" duration (CR 611.1). CR 700.2 (modal): the mode is
/// the controller's choice; MAST records the alternatives, the engine resolves
/// the pick (ADR 0004).
/// </para>
///
/// <para>
/// Anchored (^…$) and requires the exact "target creature gains your choice of …"
/// subject — the "you control" / named-subtype variants (Steel Seraph, Manifold
/// Mouse, Atraxa's Skitterfang) carry different subject tokens and do not match.
/// Distinct from <see cref="TargetCreatureGainsKeywordUntilEndOfTurnRule"/>
/// (a single fixed keyword, no choice). Any unrecognised keyword option causes a
/// bail (returns false) so the clause falls through rather than being guessed at.
/// </para>
/// </summary>
[TriggeredRule(Priority = 65)]
public sealed class TargetCreatureGainsChoiceOfKeywordsUntilEndOfTurnRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^target\s+creature\s+gains\s+your\s+choice\s+of\s+(?<options>.+?)\s+until\s+end\s+of\s+turn\.?$",
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

    var options = SplitOptions(m.Groups["options"].Value);
    if (options.Count < 2)
    {
      return false;
    }

    var modes = new List<ModalOption>(options.Count);
    foreach (var option in options)
    {
      var ability = StaticRuleHelpers.MapKeywordToStaticAbility(option);
      if (ability is null)
      {
        // Unrecognised keyword — bail so fallback handles it; no free text.
        return false;
      }

      modes.Add(new ModalOption
      {
        Ability = new SpellAbility
        {
          Effects = [new GainAbilityEffect
          {
            Target = ObjectReference.Target(new ObjectFilter { CardTypes = ["creature"] }),
            GainedAbility = ability,
            Duration = UntilTimeDuration.EndOfTurn,
          }],
        },
      });
    }

    effect = new ModalEffect
    {
      ModeSelection = ModeSelection.ChooseOne(),
      Modes = modes,
    };
    return true;
  }

  // Splits "first strike or vigilance" / "flying, vigilance, or lifelink" into
  // the ordered keyword-phrase list. Handles the Oxford-comma "…, or …", plain
  // "… or …", and comma separators; multi-word keyword phrases ("first strike")
  // are preserved because the split is on the separators, not on whitespace.
  private static List<string> SplitOptions(string raw)
  {
    var normalized = Regex.Replace(raw, @",\s*or\s+", "|");
    normalized = Regex.Replace(normalized, @"\s+or\s+", "|");
    normalized = Regex.Replace(normalized, @"\s*,\s*", "|");
    return normalized
      .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
      .ToList();
  }
}
