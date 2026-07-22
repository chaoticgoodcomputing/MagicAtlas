namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Ingest: Whenever this creature deals combat damage to a player, that player
/// exiles the top card of their library.
///
/// CR 702.115a (verbatim): "Ingest is a triggered ability. 'Ingest' means
/// 'Whenever this creature deals combat damage to a player, that player exiles
/// the top card of their library.'"
///
/// MAST shape (ADR 0003 decomposition): TriggeredAbility{ KeywordSource:"Ingest",
///   Trigger:{ Timing:Whenever, Event:DealsCombatDamageToPlayer,
///             Filter:{CardTypes:["creature"]} },
///   Effects:[ ExileEffect{ Target:{Kind:Designated,
///     Filter:{CardTypes:["card"], Zone:Library,
///             Characteristics:[Other("top"), Other("that player's")]}} } ] }.
///
/// "Top" (positional) and "that player's" (ownership back-reference to the
/// damaged player established by the trigger) are predicates that do not yet
/// have first-class ObjectFilter fields; they are carried as OtherCharacteristic
/// residuals per the ADR 0001 free-text doctrine and the Mentor/Flanking
/// convention.  The card is Designated (deterministic by position, no choice)
/// rather than Target (no targeting rule keyword in CR 702.115a).
/// </summary>
[Keyword]
public sealed class IngestKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Ingest")
    from reminder in OptionalReminder
    select (Ability)new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Ingest,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.Whenever,
        Event = TriggerEvent.DealsCombatDamageToPlayer,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      Effects =
      [
        new ExileEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Designated,
            Filter = new ObjectFilter
            {
              CardTypes = ["card"],
              Zone = Zone.Library,
              // "the top card of their library" — positional (CR 401.1) + owned by the
              // damaged player ("their" = that player, established by the trigger, CR 108.3).
              LibraryPosition = new LibraryPosition { Position = ZonePosition.Top },
              Owner = ControllerFilter.ThatPlayer,
            },
          },
        },
      ],
      Reminder = reminder,
    }
  );
}
