namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Explore (Rule 701.44a). "[Permanent] explores. (Reveal the top card of your
/// library. Put that card into your hand if it's a land. Otherwise, put a
/// +1/+1 counter on this creature, then put the card back or put it into your
/// graveyard.)"
///
/// <para>
/// MAST records the keyword action's invocation and the exploring subject; the
/// reveal / land-to-hand / +1/+1-counter / graveyard sequence is engine
/// territory per the descriptive-not-engine doctrine (mirroring the
/// SurveilEffect pattern — Surveil similarly records only the keyword and
/// count, not the look/put-into-graveyard machinery).
/// </para>
///
/// <para>
/// Filed under <c>CardFlow</c> alongside <see cref="SurveilEffect"/>: the
/// keyword's primary descriptive axis is library-reveal followed by put-into-
/// zone, with the +1/+1 counter being a fallback path. The Counter directory
/// hosts effects whose <em>primary</em> shape is counter placement; here that
/// is conditional and secondary.
/// </para>
/// </summary>
[OracleEffect("explore")]
public sealed record ExploreEffect : Effect
{
  /// <summary>
  /// The permanent doing the exploring. In the canonical ETB trigger the
  /// subject is "it" (the entering creature) — represented by
  /// <see cref="ObjectReference.It"/>.
  /// </summary>
  public required ObjectReference Target { get; init; }
}
