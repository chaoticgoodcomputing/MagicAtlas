namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.References;

/// <summary>
/// "You may play that card from exile this turn." — a one-shot permission to
/// play a specific, previously-referenced card while it sits in exile, bounded
/// to the current turn (Norin, Swift Survivalist: "Whenever a creature you
/// control becomes blocked, you may exile it. You may play that card from
/// exile this turn.").
///
/// <para>
/// "That card" is a pronoun back-reference to the object a sibling effect in
/// the same ability already exiled (typically "you may exile it" — see
/// <see cref="YouMayExileItTriggeredRule"/>), so <see cref="MayPlayTargetFromExileEffect.Target"/>
/// carries <see cref="ObjectReferenceKind.It"/> rather than a filter (ADR 0004
/// "reference not resolution"). The "you may" is a structured
/// <see cref="OptionalEffect"/> wrapper, matching the codebase's convention.
/// </para>
///
/// <para>
/// Anchored (^you may play that card from exile this turn$) so it is specific
/// to this exact composite surface and does not collide with the unrelated
/// filter-based <see cref="MagicAST.AST.Effects.CardFlow.MayPlayFromExileEffect"/>
/// static-permission shapes.
/// </para>
///
/// CR 701.13a (exile) places the card in exile; this permission is what lets
/// the controller play it from there this turn (CR 305.1 "play a land" / CR
/// 601.2 "cast a spell" normally require the hand zone).
/// </summary>
[TriggeredRule]
public sealed class YouMayPlayThatCardFromExileThisTurnTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^you\s+may\s+play\s+that\s+card\s+from\s+exile\s+this\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');

    if (!Pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new OptionalEffect
    {
      Inner = new MayPlayTargetFromExileEffect
      {
        Target = ObjectReference.It(),
        Duration = MagicAST.AST.Effects.UntilTimeDuration.EndOfTurn,
      },
    };
    return true;
  }
}
