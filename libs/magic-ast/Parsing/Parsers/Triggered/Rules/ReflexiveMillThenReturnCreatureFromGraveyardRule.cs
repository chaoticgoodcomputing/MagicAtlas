namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Parsers.Spell.Rules;

/// <summary>
/// "When you do, return target [filter] card [with mana value N or less/greater/…]
/// from your graveyard to the battlefield." — the reflexive tail of the "mill N
/// cards. When you do, reanimate …" family (Yathan Roadwatcher).
///
/// <para>
/// The preceding sentence ("mill four cards") parses to a
/// <see cref="MagicAST.AST.Effects.ZoneChange.MillEffect"/> via
/// <see cref="MillTriggeredRule"/>; the sentence-bundle dispatcher in
/// <see cref="MagicAST.Parsing.Parsers.TriggeredAbilityParser"/> feeds this rule the
/// following "When you do, …" sentence, which is a <b>reflexive triggered ability</b>
/// (CR 603.12) created when the ETB ability resolves — a separate object, not an
/// inline effect. Modelled as a <see cref="CreateDelayedTriggerEffect"/> wrapping a
/// <see cref="DelayedTriggeredAbility"/> whose trigger is "when you mill"
/// (<see cref="TriggerEvent.Mills"/>, Controller = You), mirroring the fetchland
/// reflexive shape (<see cref="ReflexiveSacrificeSearchBasicLandGainLifeRule"/>,
/// which bakes <see cref="TriggerEvent.Sacrifices"/>).
/// </para>
///
/// <para>
/// CR 603.12 (verbatim): "A resolving spell or ability may allow or instruct a
/// player to take an action and create a triggered ability that triggers 'when [a
/// player] [does or doesn't]' take that action or 'when [something happens] this
/// way.' These reflexive triggered abilities follow the rules for delayed triggered
/// abilities (see rule 603.7) …"
/// </para>
///
/// <para>
/// CR 603.7 (verbatim): "An effect may create a delayed triggered ability that can
/// do something at a later time. A delayed triggered ability will contain 'when,'
/// 'whenever,' or 'at,' although that word won't usually begin the ability."
/// CR 701.17a (Mill): "For a player to mill a number of cards, that player puts that
/// many cards from the top of their library into their graveyard."
/// </para>
///
/// <para>
/// ANCHORED (^…$): the whole "when you do, return … battlefield" sentence is anchored,
/// so it cannot substring-match into a more specific sibling. The reanimation interior
/// is delegated to <see cref="ReturnGraveyardToBattlefieldRule"/> (the same node the
/// spell parser builds for the identical surface), so the optional mana-value qualifier
/// and filter-type variants are handled in one place.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class ReflexiveMillThenReturnCreatureFromGraveyardRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^when you do,\s*(?<return>return\s+target\s+.+from\s+your\s+graveyard\s+to\s+the\s+battlefield)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly ReturnGraveyardToBattlefieldRule _returnRule = new();

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = _pattern.Match(text.Trim().TrimEnd('.').Trim());
    if (!m.Success)
    {
      return false;
    }

    // Delegate the reanimation interior to the spell-level rule so the mana-value
    // qualifier / filter-type variants are parsed in exactly one place.
    if (!_returnRule.TryMatch(m.Groups["return"].Value.Trim(), out var returnEffect)
      || returnEffect is null)
    {
      return false;
    }

    effect = new CreateDelayedTriggerEffect
    {
      DelayedTrigger = new DelayedTriggeredAbility
      {
        Trigger = new TriggerCondition
        {
          Timing = TriggerTiming.When,
          Event = TriggerEvent.Mills,
          Filter = new ObjectFilter { Controller = ControllerFilter.You },
        },
        Effects = new List<Effect> { returnEffect },
      },
    };
    return true;
  }
}
