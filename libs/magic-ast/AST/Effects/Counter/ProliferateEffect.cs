namespace MagicAST.AST.Effects.Counter;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Proliferate (Rule 701.27). "Choose any number of permanents and/or players, then give each
/// another counter of each kind already there."
///
/// <para>
/// Proliferate is a parameterless keyword action: it has no subject, target list, or quantity
/// beyond its own definition. MAST records the invocation descriptively; which permanents and
/// players are chosen is a game-state decision handled at the table.
/// </para>
/// </summary>
[OracleEffect("proliferate")]
public sealed record ProliferateEffect : Effect;
