namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "exile target nonland permanent an opponent controls until this [type] leaves the battlefield."
/// — the Oblivion Ring / Hieromancer's Cage ETB exile shape.
///
/// The effect is a temporary exile: the permanent returns when this [type] leaves the
/// battlefield (Rule 611 — continuous effects with duration). MAST records the exile
/// action descriptively (Rule 701.10) with an
/// <see cref="UntilLeavesBattlefieldDuration"/> whose <c>Object</c> is the literal
/// self-reference phrase from oracle text ("this enchantment", "this creature", etc.).
///
/// The LTB return ability is engine territory (Rule 603.7d — linked triggered abilities);
/// MAST does not model it (descriptive-not-engine doctrine).
/// </summary>
[TriggeredRule]
public sealed class ExileUntilLeavesTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^exile\s+target\s+nonland\s+permanent\s+an\s+opponent\s+controls\s+until\s+this\s+(?<type>creature|artifact|enchantment|permanent|aura|land)\s+leaves\s+the\s+battlefield$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var selfType = m.Groups["type"].Value.ToLowerInvariant();

    effect = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["permanent"],
          Characteristics = ["nonland"],
          Controller = ControllerFilter.Opponent,
        },
      },
      Duration = new UntilLeavesBattlefieldDuration
      {
        Object = $"this {selfType}",
      },
    };
    return true;
  }
}
