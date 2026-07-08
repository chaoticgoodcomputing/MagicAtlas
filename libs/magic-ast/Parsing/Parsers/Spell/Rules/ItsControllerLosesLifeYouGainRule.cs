namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Its controller loses N life and you gain M life." — the anaphoric drain pattern
/// where the controller of a permanent/spell named in a prior sentence loses a fixed
/// amount of life and the controller of this spell gains the same (or another) amount.
///
/// <para>
/// Recognised shapes:
/// <list type="bullet">
///   <item>"Its controller loses 3 life and you gain 3 life." — Punish Ignorance
///   (the trailing sentence of "Counter target spell. Its controller loses 3 life
///   and you gain 3 life.")</item>
/// </list>
/// </para>
///
/// <para>
/// "Its controller" is modelled as <see cref="ObjectReferenceKind.Controller"/>, an
/// anaphoric reference to the controller of the previously mentioned object (here the
/// countered spell). "you" is the controller of this spell, modelled as
/// <see cref="ObjectReferenceKind.You"/>. No runtime tracking is introduced — MAST
/// describes the card, it does not execute it.
/// </para>
///
/// <para>
/// Emits a flat [<see cref="LoseLifeEffect"/>, <see cref="GainLifeEffect"/>] pair via
/// <see cref="IMultiSpellRule.TryMatchMulti"/>. The single-effect
/// <see cref="ISpellRule.TryMatch"/> always returns false — this shape never reduces
/// to a single Effect. The two amounts are parsed independently and need not be equal;
/// the conjunction "and you gain M life" is what distinguishes this from the plain
/// <see cref="ItsControllerLosesLifeRule"/> ("Its controller loses N life." — Spreading
/// Rot), whose pattern anchors immediately after "life" and so never overlaps.
/// </para>
///
/// CR 119.3 (verbatim): "If an effect causes a player to gain life or lose life,
/// that player's life total is adjusted accordingly."
/// </summary>
[SpellRule]
public sealed class ItsControllerLosesLifeYouGainRule : ISpellRule, IMultiSpellRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+|X|Y|Z";

  private static readonly Regex _pattern = new(
    $@"^Its\s+controller\s+loses?\s+(?<lose>{CountTokens})\s+life\s+and\s+you\s+gain\s+(?<gain>{CountTokens})\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // -------------------------------------------------------------------------
  // ISpellRule — single-effect path intentionally disabled.
  // -------------------------------------------------------------------------
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  // -------------------------------------------------------------------------
  // IMultiSpellRule — flat [LoseLifeEffect, GainLifeEffect] pair.
  // -------------------------------------------------------------------------
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var loseQuantity = ParseQuantity(m.Groups["lose"].Value);
    var gainQuantity = ParseQuantity(m.Groups["gain"].Value);

    effects = new List<Effect>
    {
      new LoseLifeEffect
      {
        Amount = loseQuantity,
        Player = new ObjectReference { Kind = ObjectReferenceKind.Controller },
      },
      new GainLifeEffect
      {
        Amount = gainQuantity,
        Player = ObjectReference.You(),
      },
    };
    return true;
  }

  private static Quantity ParseQuantity(string raw)
  {
    var lower = raw.ToLowerInvariant();
    if (lower is "x" or "y" or "z")
    {
      return new VariableQuantity { Name = lower.ToUpperInvariant() };
    }
    return LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(raw));
  }
}
