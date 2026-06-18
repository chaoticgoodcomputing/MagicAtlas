namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Ward {cost}: A triggered ability that counters spells or abilities targeting this
/// permanent unless the opponent pays the stated cost.
///
/// CR 702.21a (verbatim): "Ward is a triggered ability. Ward [cost] means 'Whenever
/// this permanent becomes the target of a spell or ability an opponent controls,
/// counter that spell or ability unless that player pays [cost].'"
///
/// MAST shape (ADR 0003 decomposition): TriggeredAbility{ KeywordSource:"Ward",
///   Trigger:{ Timing:"Whenever", Event:"BecomesTarget",
///             Filter:{Controller:"Opponent"} },
///   Effects:[ PreventableEffect{ Inner:CounterSpellEffect{ Target:{Kind:"It"} },
///                                Unless:{ Player:{Kind:"ThatPlayer"}, Cost:ManaCost } } ] }.
///
/// <para>
/// This combinator handles only the mana-cost form of Ward (e.g. "Ward {2}",
/// "Ward {1}{G}"). The life-cost and sacrifice-cost forms are handled by the
/// existing <see cref="MagicAST.Parsing.Parsers.Static.WardKeywordRule"/> static rule
/// (Priority 989), which is reached for standalone clauses when the KeywordListRule
/// does not consume the full clause. Both produce the same TriggeredAbility shape.
/// </para>
///
/// <para>
/// MAST Priority: <see cref="KeywordTier.Parameterized"/> — must be tried after
/// all Simple keywords in the Or-chain so a bare keyword word "ward" doesn't mask
/// the parameterized "ward {cost}" form (KeywordRegistry builds Simple.Try().Or(Parameterized)).
/// </para>
/// </summary>
[Keyword]
public sealed class WardKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Ward")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select BuildAbility(new ManaCost { Symbols = cost.Symbols }, reminder)
  );

  private static Ability BuildAbility(ManaCost manaCost, MagicAST.AST.Parenthetical? reminder) =>
    new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Ward,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.Whenever,
        Event = TriggerEvent.BecomesTarget,
        Filter = new ObjectFilter { Controller = ControllerFilter.Opponent },
      },
      Effects =
      [
        new PreventableEffect
        {
          Inner = new CounterSpellEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.It },
          },
          Unless = new MagicAST.AST.Effects.UnlessClause
          {
            Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
            Cost = manaCost,
          },
        },
      ],
      Reminder = reminder,
    };
}
