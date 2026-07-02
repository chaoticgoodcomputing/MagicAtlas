namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return target [type] [you control|an opponent controls] to its owner's hand." —
/// the single-target battlefield-bounce as an activated-ability effect. Handles the
/// most common permanent types (creature, artifact, enchantment, land, permanent,
/// planeswalker) with an optional controller qualifier.
///
/// <para>
/// CR 402: returning an object to its owner's hand is a zone change from any zone to
/// the Hand zone. No dedicated keyword action; the text is stated directly.
/// </para>
///
/// <para>
/// ANCHOR: pattern is anchored (^…$) on the full clause so it does not collide with
/// the self-bounce rules (<see cref="ReturnSelfToHandEffectRule"/> at 989,
/// <see cref="ReturnNamedToHandEffectRule"/> at 988) or the "up to one target"
/// disjunction rule (<see cref="ReturnUpToOneTargetTypeDisjunctionToHandEffectRule"/>
/// at 985). This rule runs at Priority 984 — below all of those — so they get first
/// refusal on their tighter shapes.
/// </para>
///
/// <para>
/// Controller qualifier "you control" maps to <see cref="ControllerFilter.You"/>;
/// "an opponent controls" maps to <see cref="ControllerFilter.Opponent"/>; absence of
/// any qualifier leaves <see cref="ObjectFilter.Controller"/> null.
/// </para>
///
/// Rule 107.14 is not directly relevant here; this rule fires on the effect side of
/// activated abilities. The zone-change semantics are CR 402 (hand zone). The owner's
/// hand destination is the canonical phrasing for bounce (CR 109.5: a permanent's
/// "owner" is the player who started with it in their library or starting hand).
/// </summary>
[ActivatedEffectRule(Priority = 984)]
public sealed class ReturnTargetToHandEffectRule : IActivatedEffectRule
{
  private static readonly string _typeGroup =
    "creature|artifact|enchantment|land|permanent|planeswalker";

  // "Return target <type> [you control|an opponent controls] to its owner's hand."
  private static readonly Regex _pattern = new(
    @"^Return\s+target\s+(?<type>" + _typeGroup + @")\s*(?<ctrl>you\s+control|an\s+opponent\s+controls)?\s+to\s+its?\s+owner'?s\s+hand\.?$",
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

    return new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = [cardType],
          Controller = controller,
        },
      },
    };
  }
}
