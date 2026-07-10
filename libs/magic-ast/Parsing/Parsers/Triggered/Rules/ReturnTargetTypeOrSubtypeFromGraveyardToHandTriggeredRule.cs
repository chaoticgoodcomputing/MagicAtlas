namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Linq;
using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return target [X], [Y], or [Z] card from your graveyard to your hand." where
/// the disjunction MIXES a capitalised card subtype (Plant, Treefolk, ...) with a
/// base card type (land, creature, ...) — Kirri, Talented Sprout: "At the
/// beginning of each of your postcombat main phases, return target Plant,
/// Treefolk, or land card from your graveyard to your hand."
///
/// <para>
/// Reuses the mixed type-or-subtype classification
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.CounterTargetTypeOrSubtypeSpellRule"/>
/// establishes for a two-way disjunction, generalised to any count of comma/or-joined
/// members the same way <see cref="LookAtTopMayRevealMatchingRestToBottomTriggeredRule"/>
/// generalises it: known card-type words route to <see cref="ObjectFilter.CardTypes"/>;
/// capitalised non-card-type words route to <see cref="ObjectFilter.Subtypes"/> (OR
/// semantics — CR 205.3, a card has zero or more subtypes). Source zone is the
/// controller's own graveyard (CR 404.1); destination is the controller's hand
/// (CR 402.1); CR 701.10 (return, the keyword action moving an object from one
/// zone to another).
/// </para>
///
/// <para>
/// DELIBERATELY declines any disjunction whose members are ALL recognised card
/// types (e.g. "artifact or creature") — that pure-card-type shape is already
/// produced (if incompletely — no <see cref="ObjectFilter.Zone"/>/<see cref="ObjectFilter.Controller"/>)
/// by the generic <see cref="ReturnToHandRule"/> for existing cards (Interceptor
/// Mechan's "return target artifact or creature card from your graveyard to your
/// hand."). Requiring at least one genuine capitalised subtype member guarantees
/// this rule can never collide with that sibling's gold fixture — it only claims
/// the mixed shape no existing rule covers.
/// </para>
///
/// <para>
/// Priority 70, matching the sibling graveyard-return triggered rules (e.g.
/// <see cref="ReturnInstantOrSorceryFromGraveyardRule"/>), beating the generic
/// <see cref="ReturnToHandRule"/> (default priority 50) so the graveyard source
/// zone and the subtype members are not silently dropped. ANCHORED (^…$) end to
/// end so it cannot collide as a substring of any other return-effect sibling.
/// </para>
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class ReturnTargetTypeOrSubtypeFromGraveyardToHandTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^return\s+target\s+(?<filter>[A-Za-z][A-Za-z ,]*?)\s+card\s+from\s+your\s+graveyard\s+to\s+your\s+hand$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known MTG card types (singular, lowercase). A disjunction member matching one
  // of these is a card type; anything else, if capitalised (per oracle convention
  // for subtypes — CR 205.3m), is a subtype.
  private static readonly HashSet<string> _knownCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature", "artifact", "enchantment", "instant", "sorcery", "land",
    "planeswalker", "battle", "permanent",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    // Split the filter clause on ", " and/or " or " into its disjunction members
    // — "Plant, Treefolk, or land" → ["Plant", "Treefolk", "land"]. The ", or "
    // alternative MUST be tried before the plain "," alternative — a
    // plain-comma-first split leaves the Oxford-comma "or" glued onto the final
    // member.
    var members = Regex
      .Split(m.Groups["filter"].Value.Trim(), @"\s*,\s*or\s+|\s*,\s*|\s+or\s+")
      .Select(x => x.Trim())
      .Where(x => x.Length > 0)
      .ToList();
    if (members.Count == 0)
    {
      return false;
    }

    var cardTypes = new List<string>();
    var subtypes = new List<string>();
    foreach (var member in members)
    {
      if (_knownCardTypes.Contains(member))
      {
        cardTypes.Add(member.ToLowerInvariant());
      }
      else if (char.IsUpper(member[0]))
      {
        subtypes.Add(member);
      }
      else
      {
        // Neither a known card type nor a capitalised subtype word — decline
        // rather than guess.
        return false;
      }
    }

    if (subtypes.Count == 0)
    {
      // Pure card-type disjunction (e.g. "artifact or creature") — leave this to
      // the generic ReturnToHandRule so existing siblings are unaffected.
      return false;
    }

    effect = new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = cardTypes.Count > 0 ? cardTypes : null,
          Subtypes = subtypes,
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
    };
    return true;
  }
}
