namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[Self] deals N damage to each creature [and each player/opponent]." — sweeper damage.
/// Covers three shapes:
/// <list type="bullet">
///   <item>"[Self] deals N damage to each creature" — creature-only wrath (Sweltering Suns)</item>
///   <item>"[Self] deals N damage to each player [or opponent]" — player-only (Earthquake variants)</item>
///   <item>"[Self] deals N damage to each creature and each player" — full sweeper (Rain of Embers)</item>
/// </list>
/// The "each creature and each player" shape emits a <see cref="CompositeEffect"/> whose two
/// children are <see cref="DealDamageEffect"/> nodes targeting <see cref="ObjectReferenceKind.Each"/>
/// (filtered to creature) and <see cref="ObjectReferenceKind.EachPlayer"/> respectively.
/// Single-population shapes emit a bare <see cref="DealDamageEffect"/>.
/// </summary>
[SpellRule]
public sealed class DealDamageToEachRule : ISpellRule
{
  // Matches: "[Subject] deals N damage to each creature and each player/opponent"
  // Subject must start with an uppercase letter (it is the self-reference substituted card name).
  private static readonly Regex PatternCreatureAndPlayer = new(
    @"^(?<subject>\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+each\s+creature\s+and\s+each\s+(?<playerscope>player|opponent)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches: "[Subject] deals N damage to each creature"
  private static readonly Regex PatternCreatureOnly = new(
    @"^(?<subject>\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+each\s+creature$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches: "[Subject] deals N damage to each player/opponent"
  private static readonly Regex PatternPlayerOnly = new(
    @"^(?<subject>\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+each\s+(?<playerscope>player|opponent)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // Try "each creature and each player/opponent" first (most specific).
    var m = PatternCreatureAndPlayer.Match(text);
    if (m.Success && IsUpperSubject(m.Groups["subject"].Value))
    {
      var amount = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(m.Groups["amount"].Value));
      var playerKind = m.Groups["playerscope"].Value.Equals("opponent", StringComparison.OrdinalIgnoreCase)
        ? ObjectReferenceKind.EachOpponent
        : ObjectReferenceKind.EachPlayer;

      effect = new CompositeEffect
      {
        Effects =
        [
          new DealDamageEffect
          {
            Amount = amount,
            Source = ObjectReference.Self(),
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter { CardTypes = ["creature"] },
            },
          },
          new DealDamageEffect
          {
            Amount = amount,
            Source = ObjectReference.Self(),
            Target = new ObjectReference { Kind = playerKind },
          },
        ],
      };
      return true;
    }

    // Try "each creature" only.
    m = PatternCreatureOnly.Match(text);
    if (m.Success && IsUpperSubject(m.Groups["subject"].Value))
    {
      var amount = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(m.Groups["amount"].Value));
      effect = new DealDamageEffect
      {
        Amount = amount,
        Source = ObjectReference.Self(),
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Each,
          Filter = new ObjectFilter { CardTypes = ["creature"] },
        },
      };
      return true;
    }

    // Try "each player/opponent" only.
    m = PatternPlayerOnly.Match(text);
    if (m.Success && IsUpperSubject(m.Groups["subject"].Value))
    {
      var amount = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(m.Groups["amount"].Value));
      var playerKind = m.Groups["playerscope"].Value.Equals("opponent", StringComparison.OrdinalIgnoreCase)
        ? ObjectReferenceKind.EachOpponent
        : ObjectReferenceKind.EachPlayer;
      effect = new DealDamageEffect
      {
        Amount = amount,
        Source = ObjectReference.Self(),
        Target = new ObjectReference { Kind = playerKind },
      };
      return true;
    }

    return false;
  }

  private static bool IsUpperSubject(string subject) =>
    subject.Length > 0 && char.IsUpper(subject[0]);
}
