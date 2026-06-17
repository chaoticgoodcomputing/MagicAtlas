namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Kicker-gated mill-opponent-or-mill-half-library spell pattern.
/// Handles the two-sentence oracle text:
/// <code>
/// Each opponent mills [N] cards.
/// If this spell was kicked, instead each opponent mills half their library, rounded up.
/// </code>
/// Produces two sibling <see cref="MillEffect"/> nodes on
/// <see cref="MagicAST.AST.Abilities.SpellAbility.Effects"/>:
/// <list type="number">
///   <item>Base mill — count N, player EachOpponent, no condition.</item>
///   <item>Kicked replacement mill — count = half opponent's library rounded up, player
///     EachOpponent, condition = <see cref="KeywordCostPaidCondition"/> Kicker
///     (CR 702.33d–e). The "instead" is expressed by the condition: when the condition
///     holds the kicked effect supersedes the base.</item>
/// </list>
/// The kicked count is a <see cref="CalculatedQuantity"/> (operation "half", rounding
/// "up") over a <see cref="CountQuantity"/> of cards in the opponent's library.
///
/// <para>
/// CR 702.33 (Kicker): "Kicker is a static ability … 'Kicker [cost]' means 'You may pay
/// an additional [cost] as you cast this spell.'" CR 702.33d: "a spell has been kicked"
/// when its kicker cost was paid. CR 702.33e: effects gated on the kicked state are
/// "if this spell was kicked" clauses.
/// </para>
/// </summary>
[SpellRule(Priority = 80)]
public sealed class MaddeningCacophonyRule : ISpellRule, IMultiSpellRule
{
  // Sentence 1: "Each opponent mills N cards"
  private static readonly Regex Sentence1 = new(
    @"^Each\s+opponent\s+mills?\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Sentence 2: "If this spell was kicked, instead each opponent mills half their library, rounded up"
  private static readonly Regex Sentence2 = new(
    @"^If\s+this\s+spell\s+was\s+kicked,\s+instead\s+each\s+opponent\s+mills?\s+half\s+their\s+library,?\s+rounded\s+up$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Full two-sentence match split on ". " + capital
  private static readonly Regex TwoSentence = new(
    @"^(?<s1>.+?)\.\s+(?<s2>If\s+this\s+spell\s+was\s+kicked,.+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline
  );

  /// <inheritdoc/>
  /// <remarks>Returns <c>false</c> unconditionally — this shape always produces two
  /// sibling effects; callers must use <see cref="TryMatchMulti"/>.</remarks>
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  /// <inheritdoc/>
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;

    var full = text.Trim().TrimEnd('.');
    var split = TwoSentence.Match(full);
    if (!split.Success)
    {
      return false;
    }

    var s1 = split.Groups["s1"].Value.Trim();
    var s2 = split.Groups["s2"].Value.Trim().TrimEnd('.');

    var m1 = Sentence1.Match(s1);
    if (!m1.Success)
    {
      return false;
    }

    var m2 = Sentence2.Match(s2);
    if (!m2.Success)
    {
      return false;
    }

    var baseCount = SpellRuleHelpers.ParseSmallWord(m1.Groups["count"].Value);

    // Base mill effect: each opponent mills N cards (no condition — default behavior).
    var baseMill = new MillEffect
    {
      Count = LiteralQuantity.Of(baseCount),
      Player = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent },
    };

    // Kicked mill effect: each opponent mills half their library, rounded up.
    // The library size is CountQuantity over cards in the opponent's library.
    var libraryCount = new CountQuantity
    {
      CountOf = new ObjectFilter
      {
        CardTypes = ["card"],
        Zone = Zone.Library,
        Controller = ControllerFilter.Opponent,
      },
    };
    var halfLibraryRoundedUp = new CalculatedQuantity
    {
      Operation = "half",
      BaseQuantity = libraryCount,
      Rounding = "up",
    };

    var kickedMill = new MillEffect
    {
      Count = halfLibraryRoundedUp,
      Player = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent },
      Condition = new KeywordCostPaidCondition { Keyword = KeywordAbility.Kicker },
    };

    effects = [baseMill, kickedMill];
    return true;
  }
}
