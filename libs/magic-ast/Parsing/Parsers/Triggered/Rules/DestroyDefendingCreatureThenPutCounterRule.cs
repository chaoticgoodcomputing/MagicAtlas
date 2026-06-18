namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "destroy target creature defending player controls, then put a +1/+1 counter
/// on [Name]." — the compound triggered attack-effect on Grimgrin, Corpse-Born
/// and similar attack-triggered destroy-then-counter abilities.
///
/// <para>
/// Produces a single <see cref="CompositeEffect"/> containing two sibling effects
/// (consistent with the BloodArtist/multi-effect triggered convention):
/// <list type="number">
///   <item>
///     <see cref="DestroyEffect"/> targeting a <see cref="ObjectReferenceKind.Target"/>
///     creature with <see cref="ControllerFilter.DefendingPlayer"/> — "destroy
///     target creature defending player controls" (CR 701.7: destroy means put the
///     permanent into its owner's graveyard; CR 508.1b: the defending player is
///     whoever is being attacked).
///   </item>
///   <item>
///     <see cref="PutCountersEffect"/> targeting <see cref="ObjectReferenceKind.Self"/>
///     with one +1/+1 counter — "then put a +1/+1 counter on [Name]" where
///     [Name] is a self-reference by the card's own name (CR 201.5; CR 122.1).
///   </item>
/// </list>
/// </para>
///
/// <para>
/// The pattern is anchored (^…$) to prevent substring collision with
/// <see cref="DestroyTargetTriggeredRule"/>, which would otherwise consume the
/// "destroy target creature" prefix and silently drop the ", then put" clause.
/// Priority 60 — above <see cref="PutCountersTriggeredRule"/> (default 50) so
/// this anchored compound form is tried first; it will not match unless both
/// "destroy ... defending player" and "then put a +1/+1 counter" are present.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class DestroyDefendingCreatureThenPutCounterRule : ITriggeredRule
{
  // "destroy target creature defending player controls, then put a +1/+1 counter on [Name][.]"
  // The card-name portion after "on " is self-referential (CR 201.5).
  // Anchored to prevent the "destroy target creature" prefix from being claimed
  // by generic rules (PutCountersTriggeredRule, DestroyTargetTriggeredRule).
  private static readonly Regex _pattern = new(
    @"^destroy\s+target\s+creature\s+defending\s+player\s+controls,?\s+then\s+put\s+a\s+\+1/\+1\s+counter\s+on\s+[A-Z][A-Za-z'\-]+(?:,\s+[A-Za-z'\-]+(?:\s+[A-Za-z'\-]+)*)?\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    // "destroy target creature defending player controls" —
    // CR 701.7: destroy = put into owner's graveyard; CR 508.1b: defending player.
    var destroyEffect = new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.DefendingPlayer,
        },
      },
    };

    // "put a +1/+1 counter on [Name]" — self-reference by the card's own name
    // (CR 201.5). CR 122.1: a +1/+1 counter modifies the permanent's P/T.
    var putCounterEffect = new PutCountersEffect
    {
      Target = ObjectReference.Self(),
      CounterType = "+1/+1",
      Count = LiteralQuantity.Of(1),
    };

    effect = new CompositeEffect
    {
      Effects = [destroyEffect, putCounterEffect],
    };
    return true;
  }
}
