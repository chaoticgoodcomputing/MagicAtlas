namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy this &lt;type&gt;." — a self-destroy activated-ability effect (Detention
/// Vortex's "{3}: Destroy this Aura."). CR 109.2/CR 701.8a: "this &lt;type&gt;" refers
/// to the permanent with the ability (<see cref="ObjectReferenceKind.Self"/>), and
/// destroying it moves it from the battlefield to its owner's graveyard.
///
/// Distinct from <see cref="DestroyTargetEffectRule"/> ("Destroy target &lt;type&gt;"):
/// the subject here is the source permanent itself, not a chosen target, so no
/// <c>Target</c> ability is granted and no legal-target filter is needed — the
/// type word ("Aura", "creature", "artifact", …) is redundant with Self and is
/// dropped, mirroring the sibling self-reference rules in this directory
/// (e.g. <see cref="ReturnSelfFromGraveyardToBattlefieldEffectRule"/>).
/// </summary>
[ActivatedEffectRule(Priority = 601)]
public sealed class DestroySelfEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Destroy\s+this\s+(?:creature|artifact|enchantment|land|planeswalker|permanent|aura|equipment|vehicle|saga)\s*\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim();
    if (!_pattern.IsMatch(text))
    {
      return null;
    }

    return new DestroyEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
    };
  }
}
