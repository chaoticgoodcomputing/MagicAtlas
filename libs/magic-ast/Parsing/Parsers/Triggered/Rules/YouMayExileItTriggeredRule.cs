namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "you may exile it" — the optional variant of the CR 701.13a exile action on
/// the triggered side, where the trigger's subject is the object exiled (Norin,
/// Swift Survivalist: "Whenever a creature you control becomes blocked, you may
/// exile it.").
///
/// <para>
/// The "you may" is a structured <see cref="OptionalEffect"/> (the codebase's
/// convention, matching <see cref="OptionalMillTriggeredRule"/>) rather than a
/// boolean flag: wrapper presence alone encodes the optionality. Exile is a
/// one-shot action effect, so it composes directly under the wrapper with no
/// further decomposition.
/// </para>
///
/// <para>
/// Anchored (^you may exile it$) so it never collides with
/// <see cref="ExileSelfTriggeredRule"/>'s <c>^exile (it|this creature|this
/// permanent)$</c> pattern (no "you may" prefix) — the two patterns are
/// mutually exclusive by construction, so rule priority is irrelevant.
/// </para>
///
/// CR 701.13a (verbatim): "To exile an object, move it to the exile zone from
/// wherever it is."
/// </summary>
[TriggeredRule]
public sealed class YouMayExileItTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^you\s+may\s+exile\s+it$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');

    if (!Pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new OptionalEffect
    {
      Inner = new ExileEffect { Target = ObjectReference.It() },
    };
    return true;
  }
}
