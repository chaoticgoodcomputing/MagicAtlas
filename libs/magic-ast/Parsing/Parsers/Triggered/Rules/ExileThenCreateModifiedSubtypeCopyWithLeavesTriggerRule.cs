namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "exile it. If you do, create a token that's a copy of that creature, except
/// it's a [Subtype] in addition to its other types and it has \"When this token
/// leaves the battlefield, return the exiled card to its owner's graveyard.\"" —
/// Hofri Ghostforge's dies-trigger consequence.
///
/// <para>
/// UNLIKE the sibling <see cref="YouMayExileThenCreateModifiedFoodGolemCopyRule"/>
/// (Brenard, Ginger Sculptor: "you MAY exile it. If you do, …"), the exile here is
/// MANDATORY — there is no "may". Per ADR 0005 (<see cref="OptionalEffect"/>
/// wrapper presence IS the "you may"; "if you do" only rides an
/// <see cref="OptionalEffect"/> because it is otherwise meaningless without a
/// "may", CR 117.7), wrapping this mandatory exile in <see cref="OptionalEffect"/>
/// would misrepresent it as a player choice it is not. MAST describes what the
/// text says, not the corner-case runtime accounting for why WotC still writes
/// "if you do" after a mandatory action (the referenced object could already have
/// left the expected zone by the time the ability resolves) — that is exactly the
/// kind of state-based-action machinery MAST elects not to model. The two
/// instructions are therefore emitted as a flat, ordered two-effect list on the
/// SAME triggered ability (exile, then create the copy), mirroring how
/// <see cref="MagicAST.AST.Abilities.StaticAbility"/> already carries multiple
/// effects from one oracle clause (CR 603.3 — a triggered ability's effects
/// happen in the order written).
/// </para>
///
/// <para>
/// The copy carries two "except"-clause <see cref="CopyModification"/>s applied in
/// order (CR 707.2 — the copy acquires copiable values except as overridden):
/// <list type="number">
///   <item><see cref="TypeAdder"/> adds the named subtype (Capitalised, e.g.
///   Spirit) "in addition to its other types" (CR 205.1b — subtype addition
///   preserves existing ones). No power/toughness override — Hofri's copy keeps
///   the dying creature's printed P/T.</item>
///   <item><see cref="TriggeredAbilityAdder"/> adds the quoted leaves-the-
///   battlefield cleanup ability as a fully-structured <see cref="TriggeredAbility"/>
///   (CR 603.2 "[When/Whenever/At] [event], [effect]"). "This token" resolves to
///   <see cref="ObjectFilter.IsSelf"/> on the trigger filter (the granted ability's
///   own bearer, CR 109.5). "The exiled card" is the SAME card exiled by THIS
///   ability's own first effect — not linked via <see cref="ObjectFilter.ExiledWith"/>
///   (CONTRIBUTING.md "linked exile": that marker is used only when the card
///   itself prints a source reference such as "exiled with [Name]"; Hofri's text
///   says only "the exiled card", the plain definite-article back-reference to the
///   sole card this whole ability chain ever exiles), so the target is
///   <see cref="ObjectReferenceKind.Designated"/> filtered to
///   <see cref="Zone.Exile"/> alone. The move itself is a new
///   <see cref="ReturnToGraveyardEffect"/> node (no existing node covered a plain
///   exile→graveyard zone change; <see cref="ReturnToHandEffect"/>/
///   <see cref="ReturnToBattlefieldEffect"/> are the hand/battlefield analogues).</item>
/// </list>
/// </para>
///
/// <para>
/// This rule receives the ENTIRE effect body (both sentences) because the
/// multi-sentence splitter in the dispatcher declines it (the "If you do,
/// create…" second sentence has no standalone rule), falling through to the
/// single-rule dispatch — mirroring
/// <see cref="YouMayExileThenCreateModifiedFoodGolemCopyRule"/>'s framing.
/// ANCHORED (^…$) end to end on the exile-then-copy surface so it never claims a
/// substring of any sibling body. Priority 80 — matches the sibling Brenard rule's
/// band (above the generic copy rules at 70–76); the two rules are mutually
/// exclusive by construction (this one requires the mandatory "exile it." lead-in
/// with NO "you may", Brenard's requires "you may exile it.").
/// </para>
///
/// <para>
/// Rule citations: CR 603.2 (triggered ability form), CR 701.13a (exile), CR 707.2
/// (copy — copiable values), CR 205.1b (type/subtype addition), CR 400.1 (zone
/// change), CR 111.1 (token).
/// </para>
/// </summary>
[TriggeredRule(Priority = 80)]
public sealed class ExileThenCreateModifiedSubtypeCopyWithLeavesTriggerRule : ITriggeredRule
{
  // Apostrophe accepted as ASCII (') or curly (’); opening/closing quotes as ASCII (") or
  // curly (“/”). "exile it." has NO "may" — mandatory exile (contrast the sibling's
  // "you may exile it."). The subtype run is captured as a single Capitalised word.
  private static readonly Regex _pattern = new(
    @"^exile\s+it\.\s+If\s+you\s+do,\s+create\s+a\s+token\s+that['’]s\s+a\s+copy\s+of\s+that\s+creature,\s+except\s+it['’]s\s+a\s+(?<subtype>[A-Z][a-z]+)\s+in\s+addition\s+to\s+its\s+other\s+types\s+and\s+it\s+has\s+[""“]When\s+this\s+token\s+leaves\s+the\s+battlefield,\s+return\s+the\s+exiled\s+card\s+to\s+its\s+owner['’]s\s+graveyard\.[""”]$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var subtype = m.Groups["subtype"].Value;

    // "the exiled card" — the plain definite-article back-reference to the card
    // exiled by this same ability's Exile effect (see class doc — no ExiledWith
    // marker; the card's own text prints no such reference).
    var exiledCard = new ObjectReference
    {
      Kind = ObjectReferenceKind.Designated,
      Filter = new ObjectFilter { Zone = Zone.Exile },
    };

    var leavesTrigger = new TriggeredAbility
    {
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.When,
        Event = TriggerEvent.LeavesTheBattlefield,
        Filter = new ObjectFilter { IsSelf = true },
      },
      Effects = [new ReturnToGraveyardEffect { Target = exiledCard }],
    };

    effect = new CompositeEffect
    {
      Effects =
      [
        new ExileEffect { Target = ObjectReference.It() },
        new CopyEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.ThatCreature },
          Modifications =
          [
            new TypeAdder { Subtypes = [subtype] },
            new TriggeredAbilityAdder { Ability = leavesTrigger },
          ],
        },
      ],
    };
    return true;
  }
}
