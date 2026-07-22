namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile the top card of your library." — moves the top card of the
/// controller's library to the exile zone, as an activated-ability effect
/// (Mystic Forge: "{T}, Pay 1 life: Exile the top card of your library.").
///
/// <para>
/// The "top card" is a positionally-designated card — it is deterministic (no
/// player choice) and identified by position in the library, not by targeting
/// (no "target" keyword in oracle text). Therefore the reference is
/// <see cref="ObjectReferenceKind.Designated"/> rather than
/// <see cref="ObjectReferenceKind.Target"/>. The positional qualifier "top" is
/// carried as an <see cref="MagicAST.AST.References.OtherCharacteristic"/>
/// residual (the same convention used by the Ingest keyword expansion:
/// GLOSSARY §Ingest). Zone=Library and Controller=You establish which library
/// the top card comes from.
/// </para>
///
/// <para>
/// CR 701.13a: "To exile an object, move it to the exile zone from wherever
/// it is."
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 982)]
public sealed class ExileTopCardOfLibraryEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Exile\s+the\s+top\s+card\s+of\s+your\s+library\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    if (!_pattern.IsMatch(effectText))
    {
      return null;
    }

    return new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Designated,
        Filter = new ObjectFilter
        {
          CardTypes = ["card"],
          Zone = Zone.Library,
          Controller = ControllerFilter.You,
          // "the top card of your library" — positional designation (CR 401.1).
          LibraryPosition = new LibraryPosition { Position = ZonePosition.Top },
        },
      },
    };
  }
}
