namespace MagicAST.AST.Triggers;

using System.Text.Json;
using System.Text.Json.Serialization;
using MagicAST.AST.References;

/// <summary>
/// What fires a trigger: either a game <b>event</b> (a permanent dies, a spell is
/// cast) or a <b>time</b> (a clock point — "at the beginning of your upkeep").
/// ADR 0002 pulled the clock points out of the flat <see cref="TriggerEvent"/>
/// enum into <see cref="GameTime"/>; this union is the one slot a printed *or*
/// delayed trigger uses, matching CR 603's when/whenever/at.
///
/// <para>
/// Serialised heterogeneously to keep the migration small: an event is a bare
/// enum string (every existing event fixture is unchanged), a time is a bare
/// <see cref="GameTime"/> object. The implicit conversions let producer sites
/// keep writing <c>Event = TriggerEvent.Attacks</c> (or a <see cref="GameTime"/>)
/// without wrapping.
/// </para>
/// </summary>
[JsonConverter(typeof(TriggerOccurrenceConverter))]
public abstract record TriggerOccurrence
{
  public static implicit operator TriggerOccurrence(TriggerEvent @event) =>
    new EventOccurrence { Event = @event };

  public static implicit operator TriggerOccurrence(GameTime time) =>
    new TimeOccurrence { Time = time };
}

/// <summary>A trigger fired by a game event (CR 603 — zone change, combat, cast, …).</summary>
public sealed record EventOccurrence : TriggerOccurrence
{
  public required TriggerEvent Event { get; init; }
}

/// <summary>A trigger fired at a clock point (CR 603 "at" — the beginning/end of a phase or step).</summary>
public sealed record TimeOccurrence : TriggerOccurrence
{
  public required GameTime Time { get; init; }
}

/// <summary>
/// Reads/writes <see cref="TriggerOccurrence"/> heterogeneously: a JSON string is
/// an <see cref="EventOccurrence"/> (the <see cref="TriggerEvent"/> enum), a JSON
/// object is a <see cref="TimeOccurrence"/> (a <see cref="GameTime"/>).
/// </summary>
public sealed class TriggerOccurrenceConverter : JsonConverter<TriggerOccurrence>
{
  public override TriggerOccurrence Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
    reader.TokenType switch
    {
      JsonTokenType.String => new EventOccurrence
      {
        Event = JsonSerializer.Deserialize<TriggerEvent>(ref reader, options),
      },
      JsonTokenType.StartObject => new TimeOccurrence
      {
        Time = JsonSerializer.Deserialize<GameTime>(ref reader, options)
          ?? throw new JsonException("Null GameTime in trigger occurrence."),
      },
      _ => throw new JsonException(
        $"Expected a string (event) or object (time) for a trigger occurrence, got {reader.TokenType}."),
    };

  public override void Write(Utf8JsonWriter writer, TriggerOccurrence value, JsonSerializerOptions options)
  {
    switch (value)
    {
      case EventOccurrence e:
        JsonSerializer.Serialize(writer, e.Event, options);
        break;
      case TimeOccurrence t:
        JsonSerializer.Serialize(writer, t.Time, options);
        break;
      default:
        throw new JsonException($"Unknown trigger occurrence type {value.GetType().Name}.");
    }
  }
}
