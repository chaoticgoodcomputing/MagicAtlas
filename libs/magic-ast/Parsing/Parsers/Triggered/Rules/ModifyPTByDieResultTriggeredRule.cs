namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[subject] gets +X/+0 until end of turn, where X is the result[ minus N]" — a P/T
/// modification whose modifier is driven by the result of the die rolled earlier in the
/// same ability (the attack-triggered die-roll family: Velukan Dragon). The X variable is
/// resolved to the die roll's result (<see cref="DieRollResultQuantity"/>, CR 706.2), with
/// an optional "minus N" arithmetic adjustment.
///
/// <para>
/// This is the die-result-driven sibling of <see cref="ModifyPTTriggeredRule"/> (which
/// handles only literal "+N/+N" modifiers). The two are disjoint: that rule requires a
/// numeric modifier on at least one axis, this one requires a variable X on exactly one
/// axis with a "where X is the result" binding. Dispatched as one sentence of the
/// "roll a die. [subject] gets +X/+0 ..." bundle (CR 706.4 — the text indicates how to
/// use the roll result).
/// </para>
///
/// <para>
/// CR 706.2: "the final number is the result of the die roll." The "result minus N" form
/// is modeled as a <see cref="CalculatedQuantity"/> over the
/// <see cref="DieRollResultQuantity"/> base with Operation "add" and a negative
/// <c>Operand</c> (X + (-N)), reusing the established additive-offset shape rather than
/// introducing a new operation. The subject ("this creature" / "it") is the
/// triggering creature; "that creature" (the equipped/affected creature) is also accepted.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>) so it cannot match inside a longer sibling sentence. Priority 70:
/// above the literal-modifier <see cref="ModifyPTTriggeredRule"/> (default 50) so the
/// die-result form is preferred when both could superficially apply.
/// </para>
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class ModifyPTByDieResultTriggeredRule : ITriggeredRule
{
  // "<subject> gets +X/+0 [until end of turn], where X is the result[ minus N]" — the
  // variable axis may be power or toughness; the other axis is a literal modifier.
  private static readonly Regex _pattern = new(
    @"^(?<subject>this\s+creature|that\s+creature|it)\s+gets\s+"
      + @"(?<p>[+-](?:X|\d+))/(?<t>[+-](?:X|\d+))"
      + @"(?<duration>\s+until\s+end\s+of\s+turn)?,\s*"
      + @"where\s+X\s+is\s+the\s+result(?:\s+(?<op>minus|plus)\s+(?<n>\d+))?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim().TrimEnd('.').Trim());
    if (!m.Success)
    {
      return false;
    }

    var pRaw = m.Groups["p"].Value;
    var tRaw = m.Groups["t"].Value;

    // Exactly one axis must be the X variable; the other must be a literal. Two X axes or
    // zero X axes are not this shape (the latter is the literal ModifyPTTriggeredRule).
    var pIsX = pRaw.Contains('X', StringComparison.OrdinalIgnoreCase);
    var tIsX = tRaw.Contains('X', StringComparison.OrdinalIgnoreCase);
    if (pIsX == tIsX)
    {
      return false;
    }

    // Build the X quantity: the die result, optionally offset by "minus/plus N".
    Quantity xQuantity = new DieRollResultQuantity();
    if (m.Groups["op"].Success && int.TryParse(m.Groups["n"].Value, out var n) && n != 0)
    {
      var signed = m.Groups["op"].Value.Equals("minus", StringComparison.OrdinalIgnoreCase) ? -n : n;
      xQuantity = new CalculatedQuantity
      {
        BaseQuantity = new DieRollResultQuantity(),
        Operation = "add",
        Operand = signed,
      };
    }

    var target = m.Groups["subject"].Value.ToLowerInvariant() switch
    {
      "it" => ObjectReference.It(),
      _ => ObjectReference.Self(),
    };

    var power = pIsX ? xQuantity : LiteralQuantity.Of(int.Parse(pRaw));
    var toughness = tIsX ? xQuantity : LiteralQuantity.Of(int.Parse(tRaw));

    MagicAST.AST.Effects.Duration? duration =
      m.Groups["duration"].Success ? UntilTimeDuration.EndOfTurn : null;

    effect = new ModifyPTEffect
    {
      Target = target,
      PowerModifier = power,
      ToughnessModifier = toughness,
      Duration = duration,
    };
    return true;
  }
}
