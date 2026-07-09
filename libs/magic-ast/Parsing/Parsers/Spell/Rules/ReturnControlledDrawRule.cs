namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Two-sentence compound spell:
/// "Return target nonland permanent to its owner's hand. If you controlled that permanent, draw a card."
/// (Boomerang Basics).
///
/// <para>
/// Sentence 1 is a <see cref="ReturnToHandEffect"/> targeting "nonland permanent".
/// Sentence 2 is a <see cref="DrawCardsEffect"/> guarded by a
/// <see cref="ControlledObjectCondition"/> — "you controlled that permanent", where
/// "that permanent" is the object sentence 1 returned (<see cref="ObjectReferenceKind.It"/>)
/// and the controller is "you" (<see cref="ObjectReferenceKind.You"/>). Both effects sit as
/// siblings on the single <see cref="SpellAbility.Effects"/> list; the condition is
/// descriptive, not a runtime branch — consistent with the MAST "describes cards, does not
/// execute them" doctrine.
/// </para>
///
/// <para>
/// CR 121.1 (draw): "A player draws a card by putting the top card of their library into their
/// hand." CR 109.4 (controller): "Only objects on the stack or on the battlefield have a
/// controller. Objects that are neither on the stack nor on the battlefield aren't controlled
/// by any player." — hence the past-tense gate on the already-returned permanent (see
/// <see cref="ControlledObjectCondition"/>). Priority 80 so this supersedes the general
/// sentence-bundle path, which would dispatch each sentence independently and fail on the
/// conditional-draw fragment. Anchored on the "If you controlled that permanent," lead so it
/// never collides with the kicker sibling (<see cref="ReturnKickedDrawRule"/>).
/// </para>
/// </summary>
[SpellRule(Priority = 80)]
public sealed class ReturnControlledDrawRule : ISpellRule, IMultiSpellRule
{
  // Sentence 1: "Return target [mod] [type] to its owner's hand"
  // Accepts: "nonland permanent", "permanent", "nonland creature", etc.
  private static readonly Regex Sentence1 = new(
    @"^Return\s+target\s+(?<mod>non\w+\s+)?(?<type>creature|artifact|enchantment|land|permanent|planeswalker)s?\s+to\s+its?\s+owner'?s\s+hands?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Sentence 2: "If you controlled that permanent, draw a card" (exactly one card)
  private static readonly Regex Sentence2 = new(
    @"^If\s+you\s+controlled\s+that\s+permanent,\s+draw\s+(?<count>a|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Full two-sentence oracle text split boundary (". " followed by the conditional lead).
  private static readonly Regex TwoSentence = new(
    @"^(?<s1>.+?)\.\s+(?<s2>If\s+you\s+controlled\s+that\s+permanent,.+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
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

    // --- ReturnToHandEffect ---
    var modWord = m1.Groups["mod"].Value.Trim().ToLowerInvariant(); // e.g. "nonland"
    var typeWord = m1.Groups["type"].Value.ToLowerInvariant();      // e.g. "permanent"

    var characteristics = string.IsNullOrEmpty(modWord)
      ? null
      : (IReadOnlyList<string>)new[] { modWord };

    var returnEffect = new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = QualifierAxisMapper.Apply(
          new ObjectFilter { CardTypes = [typeWord] },
          characteristics
        ),
      },
    };

    // --- DrawCardsEffect gated on "you controlled that permanent" ---
    var drawCount = SpellRuleHelpers.ParseSmallWord(m2.Groups["count"].Value);

    var drawEffect = new DrawCardsEffect
    {
      Count = LiteralQuantity.Of(drawCount),
      Player = ObjectReference.You(),
      Condition = new ControlledObjectCondition
      {
        Reference = ObjectReference.It(),
        Controller = ObjectReference.You(),
      },
    };

    effects = [returnEffect, drawEffect];
    return true;
  }
}
