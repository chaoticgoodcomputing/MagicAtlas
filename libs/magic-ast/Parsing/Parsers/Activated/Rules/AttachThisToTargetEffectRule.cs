namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Attach this [Equipment|Aura|Fortification] to target [type] [you control|an
/// opponent controls]." — an explicit activated-ability re-attach instruction,
/// distinct from the Equip/Fortify/Reconfigure keyword abilities (Rule 701.3: "To
/// take an Aura, Equipment, or Fortification from where it currently is and put it
/// onto a specified object or player.").
///
/// <para>
/// Paradigm card: Bloodthirsty Blade — "{1}: Attach this Equipment to target
/// creature an opponent controls. Activate only as a sorcery." The "Activate only
/// as a sorcery" restriction sentence is stripped by
/// <see cref="ActivatedAbilityParser"/>'s shared restriction pre-pass before this
/// rule ever sees the text.
/// </para>
///
/// <para>
/// Anchored (^…$) so it only recognises the bare "Attach this [type] to target
/// [filter]." sentence, not compound effect text.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 60)]
public sealed class AttachThisToTargetEffectRule : IActivatedEffectRule
{
  private static readonly string _attachableTypeGroup = "Equipment|Aura|Fortification";
  private static readonly string _targetTypeGroup =
    "creature|artifact|enchantment|land|permanent|planeswalker|battle";

  // "Attach this <Equipment|Aura|Fortification> to target <type> [you control|an opponent controls]."
  private static readonly Regex _pattern = new(
    @"^Attach\s+this\s+(?:" + _attachableTypeGroup + @")\s+to\s+target\s+(?<type>"
      + _targetTypeGroup
      + @")\s*(?<ctrl>you\s+control|an\s+opponent\s+controls)?\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim();
    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return null;
    }

    var cardType = m.Groups["type"].Value.ToLowerInvariant();
    var ctrlRaw = m.Groups["ctrl"].Value.Trim().ToLowerInvariant();

    ControllerFilter? controller = ctrlRaw switch
    {
      var s when s.Contains("you control") => ControllerFilter.You,
      var s when s.Contains("opponent") => ControllerFilter.Opponent,
      _ => null,
    };

    return new AttachEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = [cardType], Controller = controller },
      },
    };
  }
}
