namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Linq;
using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may exile it. If you do, create a token that's a copy of that creature, except
/// it's a [P]/[T] [Types…] in addition to its other types and it has \"[activated
/// ability]\"" — Brenard, Ginger Sculptor's dies-trigger consequence.
///
/// <para>
/// The whole two-sentence body forms ONE structured <see cref="OptionalEffect"/>
/// (CR 118.12 — "[A player] may [do something]. If [that player] [does], [effect]"): the
/// <see cref="OptionalEffect.Inner"/> is the optional exile (CR 701.13a) of the triggering
/// creature ("it" → <see cref="ObjectReferenceKind.It"/>), and the "If you do" consequence
/// (<see cref="OptionalEffect.IfYouDo"/>) is the copy-token creation. The copy references
/// the dying creature by "that creature" (<see cref="ObjectReferenceKind.ThatCreature"/> —
/// the object the trigger's filter identified, exiled by the Inner effect).
/// </para>
///
/// <para>
/// The copy carries three "except"-clause <see cref="CopyModification"/>s applied in order
/// (CR 707.2 — the copy acquires copiable values except as overridden):
/// <list type="number">
///   <item><see cref="PowerToughnessOverride"/> fixes the token at [P]/[T] regardless of
///   the original's stats.</item>
///   <item><see cref="TypeAdder"/> adds card types (lowercase, e.g. artifact/creature) and
///   subtypes (Capitalised, e.g. Food/Golem) "in addition to its other types"
///   (CR 205.1b — subtype/type addition preserves existing ones).</item>
///   <item><see cref="ActivatedAbilityAdder"/> adds the quoted activated ability
///   "{2}, {T}, Sacrifice this token: You gain 3 life." as a fully-structured
///   <see cref="ActivatedAbility"/> (CR 602.1 — "[Cost]: [Effect]"; the sacrifice targets
///   the token itself, <see cref="ObjectFilter.IsSelf"/>).</item>
/// </list>
/// </para>
///
/// <para>
/// This rule receives the ENTIRE effect body (both sentences) because the multi-sentence
/// splitter in the dispatcher declines it (the "If you do, create…" second sentence has no
/// standalone rule), falling through to the single-rule dispatch. ANCHORED (^…$) end to
/// end on the exile-then-copy surface so it never claims a substring of any sibling body.
/// Priority 80 — above the generic copy rules (70–76) and the bare "you may exile it"
/// rule (which is anchored to that phrase alone and cannot match this longer body).
/// </para>
///
/// <para>
/// Rule citations: CR 118.12 ("you may … if you do"), CR 701.13a (exile), CR 707.2 (copy —
/// copiable values), CR 205.1b (type/subtype addition), CR 602.1 (activated ability form),
/// CR 111.1 (token), CR 119.3 (life gain).
/// </para>
/// </summary>
[TriggeredRule(Priority = 80)]
public sealed class YouMayExileThenCreateModifiedFoodGolemCopyRule : ITriggeredRule
{
  // Apostrophe accepted as ASCII (') or curly (’); opening/closing quotes as ASCII (") or
  // curly (“/”). The types run is captured whole and split into card types (lowercase) and
  // subtypes (Capitalised) by the handler.
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+exile\s+it\.\s+If\s+you\s+do,\s+create\s+a\s+token\s+that['’]s\s+a\s+copy\s+of\s+that\s+creature,\s+except\s+it['’]s\s+a\s+(?<cp>\d+)/(?<ct>\d+)\s+(?<types>[A-Za-z]+(?:\s+[A-Za-z]+)*?)\s+in\s+addition\s+to\s+its\s+other\s+types\s+and\s+it\s+has\s+[""“]\{(?<mana>\d+)\},\s+\{T\},\s+Sacrifice\s+this\s+token:\s+You\s+gain\s+(?<life>\d+)\s+life\.[""”]$",
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

    var copyPower = int.Parse(m.Groups["cp"].Value);
    var copyToughness = int.Parse(m.Groups["ct"].Value);
    var manaAmount = int.Parse(m.Groups["mana"].Value);
    var lifeAmount = int.Parse(m.Groups["life"].Value);

    // "Food Golem artifact creature" → subtypes (Capitalised) vs card types (lowercase),
    // in oracle order. CR 205.1b: added types/subtypes are additive.
    var words = m.Groups["types"].Value
      .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    var cardTypes = words.Where(w => char.IsLower(w[0])).Select(w => w.ToLowerInvariant()).ToList();
    var subtypes = words.Where(w => char.IsUpper(w[0])).ToList();

    var typeAdder = new TypeAdder
    {
      CardTypes = cardTypes.Count > 0 ? cardTypes : null,
      Subtypes = subtypes.Count > 0 ? subtypes : null,
    };

    var grantedAbility = new ActivatedAbility
    {
      Costs =
      [
        new ManaCost { Symbols = [ManaSymbol.Generic(manaAmount)] },
        new TapCost(),
        new SacrificeCost
        {
          Filter = new ObjectFilter { IsSelf = true },
          Quantity = LiteralQuantity.Of(1),
        },
      ],
      Effects =
      [
        new GainLifeEffect
        {
          Amount = LiteralQuantity.Of(lifeAmount),
          Player = ObjectReference.You(),
        },
      ],
      IsManaAbility = false,
    };

    effect = new OptionalEffect
    {
      Inner = new ExileEffect { Target = ObjectReference.It() },
      IfYouDo = new CopyEffect
      {
        Target = new ObjectReference { Kind = ObjectReferenceKind.ThatCreature },
        Modifications =
        [
          new PowerToughnessOverride
          {
            Power = LiteralQuantity.Of(copyPower),
            Toughness = LiteralQuantity.Of(copyToughness),
          },
          typeAdder,
          new ActivatedAbilityAdder { Ability = grantedAbility },
        ],
      },
    };
    return true;
  }
}
