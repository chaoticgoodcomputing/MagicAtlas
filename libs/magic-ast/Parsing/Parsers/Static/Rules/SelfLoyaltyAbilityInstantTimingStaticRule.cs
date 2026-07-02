namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "You may activate loyalty abilities of [Self-name] on any player's turn any time
/// you could cast an instant." — Teferi, Master of Time. A static ability on a
/// planeswalker that relaxes the loyalty-ability activation timing restriction (CR 606.3)
/// specifically for <em>this</em> planeswalker, granting instant-speed activation on any
/// player's turn.
///
/// <para>
/// CR 606.3 (verbatim): "A player may activate a loyalty ability of a permanent they
/// control any time they have priority and the stack is empty during a main phase of
/// their turn, but only if no player has previously activated a loyalty ability of that
/// permanent that turn."
/// </para>
///
/// <para>
/// This static ability removes the "during a main phase of their turn" restriction for
/// this planeswalker specifically. The "any player's turn" and "any time you could cast
/// an instant" clauses together grant timing equivalent to instant-speed on any turn,
/// recorded as <see cref="TimingModificationEffect.WhoseTurn"/> = "AnyTurn" and
/// <see cref="TimingModificationEffect.Timing"/> = <see cref="TimingWindow.Instant"/>.
/// The <see cref="TimingModificationEffect.AppliesTo"/> is keyed on
/// <see cref="ObjectActivatedAbilityReference"/> with <see cref="ObjectFilter.IsSelf"/>
/// = true — restricting the grant to this permanent's own loyalty abilities.
/// </para>
///
/// <para>
/// ANCHORED (^...$): the phrase "loyalty abilities" could appear in other static
/// ability text. The self-reference pattern ("of [word(s)]") is specific enough that
/// anchoring is the primary guard, but the IsSelf filter in the output also isolates
/// this rule to self-referencing planeswalker forms.
/// Priority 989 — precedes generic timing-grant rules.
/// </para>
/// </summary>
[StaticRule(Priority = 989)]
public sealed class SelfLoyaltyAbilityInstantTimingStaticRule : IStaticRule
{
  // "You may activate loyalty abilities of [Name] on any player's turn any time
  //  you could cast an instant."
  // [Name] is a word-sequence (possibly multi-word) ending before "on any player's".
  // Apostrophe and curly apostrophe allowed (u+2019).
  private static readonly Regex _pattern = new(
    // Name slot CANNOT span "you control": that excludes the collective sibling form ("...loyalty
    // abilities of planeswalkers you control...") which is a DIFFERENT scope (each planeswalker you
    // control), not a self-reference. Only a self-name ("Teferi, Master of Time" — no "you control")
    // matches here and licenses the IsSelf:true output.
    @"^\s*You\s+may\s+activate\s+loyalty\s+abilities\s+of\s+(?:(?!\byou\s+control\b).)+?\s+on\s+any\s+player['']s\s+turn\s+any\s+time\s+you\s+could\s+cast\s+an\s+instant\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new TimingModificationEffect
          {
            Modification = TimingModificationType.Grant,
            Timing = TimingWindow.Instant,
            WhoseTurn = "AnyTurn",
            AppliesTo = new ObjectActivatedAbilityReference
            {
              PermanentFilter = new ObjectFilter
              {
                IsSelf = true,
              },
            },
          },
        ],
      },
    ];
  }
}
