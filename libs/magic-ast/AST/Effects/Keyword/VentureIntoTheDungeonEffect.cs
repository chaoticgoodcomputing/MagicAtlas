namespace MagicAST.AST.Effects.Keyword;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Venture into the dungeon" keyword action (CR 701.49 "Venture into the Dungeon").
///
/// <para>
/// CR 701.49a–c: when a player is instructed to venture into the dungeon, they either
/// bring a dungeon card into the command zone and place their venture marker on the
/// topmost room (if they own none), or advance their venture marker to the next room
/// (following the arrows), or — from a bottommost room — complete and remove that
/// dungeon and begin a new one. The printed reminder text summarises this as
/// "Enter the first room or advance to the next room."
/// </para>
///
/// <para>
/// MAST records the keyword-action invocation descriptively; the dungeon-card
/// selection, venture-marker movement, and room-ability triggering are engine
/// territory — the node names the action, not the execution. This mirrors the
/// bare keyword-action shape of <see cref="AmassEffect"/> / <see cref="AdaptEffect"/>
/// (Keyword directory) and <c>InvestigateEffect</c>: the acting player is the
/// ability's controller (implied "you"), so no explicit subject is carried.
/// </para>
/// </summary>
[OracleEffect("ventureIntoTheDungeon")]
public sealed record VentureIntoTheDungeonEffect : Effect;
