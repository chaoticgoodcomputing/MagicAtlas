namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

/// <summary>
/// Reveal-or-enters-tapped land ("Snarl" cycle, Strixhaven):
/// "As this land enters, you may reveal a [TypeA] or [TypeB] card from your
/// hand. If you don't, this land enters tapped."
///
/// <para>Modelled descriptively as the checkland/fastland shape — the composite
/// "as this enters, tap it" (<c>When = StaticTimingKind.AsThisEnters</c> on the
/// <see cref="StaticAbility"/>, a plain <c>TapEffect</c> targeting self) gated by
/// <see cref="StaticAbility.Condition"/>. The condition names the predicate under
/// which the land enters UNtapped (the controller revealing a qualifying card):
/// the land enters tapped whenever that does not happen. The "you may reveal"
/// choice is recorded as the condition's text rather than as a game action — MAST
/// describes the card, it does not execute the reveal (descriptive-not-engine
/// doctrine).</para>
///
/// <para>The reveal is a pure entry gate carrying no quantified resource, so it
/// collapses cleanly onto the <c>Condition</c> axis. This differs from the
/// painland/shockland template (<see cref="PainlandRule"/>) where the optional
/// action is a life payment — a quantified cost that warrants its own
/// <c>PayLifeEffect</c>.</para>
/// </summary>
[StaticRule(Priority = 961)]
public sealed class RevealOrEntersTappedRule : IStaticRule
{
  // "As this [permanent] enters, you may reveal a [TypeA] (or [TypeB])? card
  // from your hand. If you don't, [it|this [permanent]] enters tapped."
  private static readonly Regex _revealOrTappedPattern = new(
    @"^\s*As\s+this\s+(?:permanent|land|creature|artifact|enchantment)\s+enters,\s+"
    + @"you\s+may\s+(?<reveal>reveal\s+an?\s+.+?\s+card\s+from\s+your\s+hand)\.\s+"
    + @"If\s+you\s+don'?t,\s+(?:it|this\s+(?:permanent|land|creature|artifact|enchantment))\s+enters\s+tapped\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _revealOrTappedPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var revealText = match.Groups["reveal"].Value.Trim();

    return
    [
      new StaticAbility
      {
        When = StaticTimingKind.AsThisEnters,
        Effects = [new MagicAST.AST.Effects.Control.TapEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
        }],
        Condition = new Condition { Text = $"you {revealText}" },
      },
    ];
  }
}
