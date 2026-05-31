namespace MagicAST.AST.Effects.Core;

using MagicAST.AST.Effects;

/// <summary>
/// Conditional clause-modifier wrapping (ADR 0005). Producers that detect a
/// "you may" / "unless [player] pays" clause wrap their effect through these so a
/// wrapper is emitted only when the clause is actually present — keeping the
/// modifier off the effect node itself.
/// </summary>
public static class EffectWrap
{
  /// <summary>Wrap in <see cref="OptionalEffect"/> iff the effect is optional or has a follow-up.</summary>
  public static Effect Optional(Effect inner, bool isOptional, Effect? ifYouDo = null, Effect? ifYouDoNot = null)
    => isOptional || ifYouDo is not null || ifYouDoNot is not null
      ? new OptionalEffect { Inner = inner, IfYouDo = ifYouDo, IfYouDoNot = ifYouDoNot }
      : inner;

  /// <summary>Wrap in <see cref="PreventableEffect"/> iff an unless-clause is present.</summary>
  public static Effect Preventable(Effect inner, UnlessClause? unless)
    => unless is not null ? new PreventableEffect { Inner = inner, Unless = unless } : inner;
}
