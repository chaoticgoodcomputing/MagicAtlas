namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Until end of turn, target creature you control becomes a [color] [card type] in
/// addition to its other colors and types." — Unctus, Grand Metatect's activated
/// ability. Additively grants BOTH a color (CR 105.3 "in addition to its other
/// colors") and a card type (CR 205.1a "in addition to its other types") to a
/// targeted creature, for the duration.
///
/// <para>
/// Distinct from <see cref="BecomesCreatureEffectRule"/> (the Keyrune/manland
/// template): that rule REPLACES the source's full characteristic set with a
/// specified P/T + colors + subtype + card types + keywords box. This clause names
/// no P/T, no subtype, no keyword — it ADDS exactly one color and one card type to an
/// ALREADY-a-creature target, which already has its own colors/types. Two distinct,
/// reusable additive-axis nodes model this faithfully: <see cref="AddColorEffect"/>
/// (layer 5) and <see cref="AddTypeEffect"/> (layer 4), sharing the same target and
/// duration — mirroring the established multi-effect-per-clause convention
/// (<c>AttachedModifyPTAndCardTypeRule</c>: one static ability, two sibling effects
/// from one oracle sentence) rather than a bespoke "becomes X in addition" node.
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the two effects sit as a
/// flat sibling pair on <c>Effects</c> rather than nested under a
/// <see cref="MagicAST.AST.Effects.Core.CompositeEffect"/>. <see cref="TryMatch"/>
/// always returns null — this shape always produces two sibling effects, so it is
/// served exclusively via <see cref="TryMatchMulti"/>.
/// </para>
///
/// <para>
/// ANCHORED (^...$) so it can't claim a substring of a longer/different clause.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 900)]
public sealed class BecomesColorAndTypeInAdditionEffectRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Until\s+end\s+of\s+turn,\s+target\s+creature\s+you\s+control\s+becomes\s+an?\s+(?<color>white|blue|black|red|green)\s+(?<type>artifact|enchantment|land|planeswalker|battle)\s+in\s+addition\s+to\s+its\s+other\s+colors\s+and\s+types$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _colorCodes =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };

  /// <inheritdoc/>
  /// <remarks>
  /// Always returns null — this shape always produces two sibling effects, so it is
  /// served exclusively via <see cref="TryMatchMulti"/>.
  /// </remarks>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var trimmed = effectText.Trim().TrimEnd('.');
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return false;
    }

    if (!_colorCodes.TryGetValue(match.Groups["color"].Value, out var colorCode))
    {
      return false;
    }

    var cardType = match.Groups["type"].Value.ToLowerInvariant();

    // A fresh ObjectReference per effect (records are immutable; the two targets
    // are value-equal): "target creature you control".
    ObjectReference Target() => new()
    {
      Kind = ObjectReferenceKind.Target,
      Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
    };

    effects = new List<Effect>
    {
      new AddColorEffect
      {
        Target = Target(),
        Colors = [colorCode],
        Duration = MagicAST.AST.Effects.UntilTimeDuration.EndOfTurn,
      },
      new AddTypeEffect
      {
        Target = Target(),
        AddedCardTypes = [cardType],
        Duration = MagicAST.AST.Effects.UntilTimeDuration.EndOfTurn,
      },
    };
    return true;
  }
}
