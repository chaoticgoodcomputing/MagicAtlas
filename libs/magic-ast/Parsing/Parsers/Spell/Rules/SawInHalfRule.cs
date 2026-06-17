namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target creature. If that creature dies this way, its controller creates two tokens
/// that are copies of that creature, except their power is half that creature's power and their
/// toughness is half that creature's toughness. Round up each time." — the Saw in Half
/// destroy-and-create-halved-copy-tokens pattern (CLB).
///
/// <para>
/// Produces a flat two-element effect list:
/// <list type="number">
///   <item>A <see cref="DestroyEffect"/> targeting the creature.</item>
///   <item>A <see cref="ConditionalEffect"/> whose condition is the
///   <see cref="OtherCondition"/> "that creature dies this way" and whose
///   <c>Then</c> branch is a <see cref="CopyEffect"/> — Count=2,
///   Target=<c>{Kind:"It"}</c>, Player=<c>{Kind:"Controller"}</c>, and
///   a single <see cref="PowerToughnessOverride"/> modification encoding
///   "half [stat] rounded up" via <see cref="CalculatedQuantity"/>.</item>
/// </list>
/// </para>
///
/// <para>
/// CR 701.7a ("To destroy a permanent, move it to its owner's graveyard") + CR 603.6
/// (the "if [preceding effect] [happened] this way" conditional — an effect-level
/// gate checked after the destroy resolves). The "Round up each time" sentence
/// is consumed structurally as <c>Rounding="up"</c> on each <see cref="CalculatedQuantity"/>.
/// </para>
/// </summary>
[SpellRule(Priority = 75)]
public sealed class SawInHalfRule : ISpellRule, IMultiSpellRule
{
  /// <summary>
  /// Matches the full oracle text of Saw in Half (trailing period stripped by the dispatcher).
  /// The "Round up each time" trailing sentence is consumed by the pattern and encoded
  /// structurally as Rounding="up" on each CalculatedQuantity.
  /// </summary>
  private static readonly Regex _pattern = new(
    @"^Destroy\s+target\s+creature\.\s+"
    + @"If\s+that\s+creature\s+dies\s+this\s+way,\s+"
    + @"its\s+controller\s+creates\s+two\s+tokens\s+that\s+are\s+copies\s+of\s+that\s+creature,\s+"
    + @"except\s+their\s+power\s+is\s+half\s+that\s+creature's\s+power\s+and\s+"
    + @"their\s+toughness\s+is\s+half\s+that\s+creature's\s+toughness\.\s+"
    + @"Round\s+up\s+each\s+time$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // ISpellRule — single-effect path intentionally disabled; always multi.
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  // IMultiSpellRule — flat [DestroyEffect, ConditionalEffect].
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    var target = new ObjectReference
    {
      Kind = ObjectReferenceKind.Target,
      Filter = new ObjectFilter { CardTypes = ["creature"] },
    };

    var destroy = new DestroyEffect { Target = target };

    // "half that creature's power, round up" → CalculatedQuantity
    var halfPowerRoundedUp = new CalculatedQuantity
    {
      BaseQuantity = new DerivedQuantity { DerivedFrom = DerivedKind.Power },
      Operation = "half",
      Rounding = "up",
    };

    // "half that creature's toughness, round up" → CalculatedQuantity
    var halfToughnessRoundedUp = new CalculatedQuantity
    {
      BaseQuantity = new DerivedQuantity { DerivedFrom = DerivedKind.Toughness },
      Operation = "half",
      Rounding = "up",
    };

    var copyEffect = new CopyEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.It },
      Count = LiteralQuantity.Of(2),
      Player = new ObjectReference { Kind = ObjectReferenceKind.Controller },
      Modifications =
      [
        new PowerToughnessOverride
        {
          Power = halfPowerRoundedUp,
          Toughness = halfToughnessRoundedUp,
        },
      ],
    };

    var conditional = new ConditionalEffect
    {
      Condition = new DiedThisWayCondition
      {
        Reference = new ObjectReference { Kind = ObjectReferenceKind.It },
      },
      Then = copyEffect,
    };

    effects = [destroy, conditional];
    return true;
  }
}
