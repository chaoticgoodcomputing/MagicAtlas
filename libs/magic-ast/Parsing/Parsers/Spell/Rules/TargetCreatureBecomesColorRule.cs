namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Recognises the spell shape "Target creature becomes [color] until end of turn."
/// (Niveous Wisps) — the SINGULAR-targeting counterpart of
/// <see cref="TargetsBecomeColorRule"/> (which handles the plural "One or more target
/// creatures become ..."). A single-target layer-5 color-changing continuous effect.
///
/// <para>
/// CR 105.3 (verbatim): "Effects may change an object's color or give a color to a
/// colorless object. If an effect gives an object a new color, the new color replaces
/// all previous colors the object had (unless the effect said the object became that
/// color 'in addition' to its other colors). Effects may also make a colored object
/// become colorless."
/// </para>
///
/// <para>
/// CR 611.1 (verbatim, partial): "Some effects instruct a player to make a choice
/// that's used to determine a game rule or the value of an effect. ... An effect
/// that changes an object's color ... generates a continuous effect."
/// </para>
///
/// <para>
/// Reuses the existing <see cref="ChangeColorEffect"/> node (<c>[OracleEffect("changeColor")]</c>)
/// — the same node emitted by the activated-ability <c>ChangeColorEffectRule</c>
/// (Metathran Transport: "{U}: Target creature becomes blue until end of turn.") — with
/// the same color-name→WUBRG map and <see cref="UntilTimeDuration.EndOfTurn"/> duration.
/// Anchored (^…$) on the bare fragment so it composes cleanly inside a sentence bundle
/// (Niveous Wisps: "Target creature becomes white until end of turn. Tap that creature.").
/// </para>
/// </summary>
[SpellRule]
public sealed class TargetCreatureBecomesColorRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Target creature becomes (?<color>white|blue|black|red|green|colorless) until end of turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _colorCodes =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"]     = "W",
      ["blue"]      = "U",
      ["black"]     = "B",
      ["red"]       = "R",
      ["green"]     = "G",
      ["colorless"] = "C",
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var match = _pattern.Match(text.Trim());
    if (!match.Success)
    {
      return false;
    }

    var colorName = match.Groups["color"].Value;
    if (!_colorCodes.TryGetValue(colorName, out var colorCode))
    {
      return false;
    }

    effect = new ChangeColorEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      Colors = [colorCode],
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
