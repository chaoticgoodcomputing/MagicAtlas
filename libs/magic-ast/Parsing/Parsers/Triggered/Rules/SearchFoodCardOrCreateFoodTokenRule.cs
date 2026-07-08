namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Courier of Comestibles: "you may search your library for a Food card, reveal it,
/// put it into your hand, then shuffle. If you don't put a card into your hand this way,
/// create a Food token."
///
/// <para>
/// A single-effect "you may [tutor], else [predefined token]" body. Decomposes — mirroring
/// the established Smothering Tithe convention (<see cref="ThatPlayerMayPayYouCreateTreasureRule"/>) —
/// into an <see cref="OptionalEffect"/> whose:
/// <list type="bullet">
///   <item><c>Inner</c> is the tutor: a <see cref="SearchLibraryEffect"/> for a Food card
///     (<c>Subtypes: ["Food"]</c>, CR 205.3g Food is an artifact subtype), revealed, to hand
///     (CR 701.23a — to search is to look through a zone for a card matching the description).</item>
///   <item><c>IfYouDoNot</c> is the "if you don't put a card into your hand this way" fallback:
///     a <see cref="CreateTokenEffect"/> for one predefined Food token
///     (CR 111.10b: "A Food token is a colorless Food artifact token with
///     '{2}, {T}, Sacrifice this token: You gain 3 life.'").</item>
/// </list>
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>) over the FULL two-sentence body so no sibling search or
/// create-token surface is mislabelled. The dispatcher's sentence-bundle splitter
/// first tries the two sentences independently; sentence two ("If you don't …")
/// matches no standalone rule, so the bundle returns null and this whole-body rule
/// is reached in the single-rule loop.
/// </para>
/// </summary>
[TriggeredRule(Priority = 65)]
public sealed class SearchFoodCardOrCreateFoodTokenRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^you\s+may\s+search\s+your\s+library\s+for\s+a\s+Food\s+card,\s*"
      + @"reveal\s+it,\s*put\s+it\s+into\s+your\s+hand,\s*then\s+shuffle\.\s*"
      + @"If\s+you\s+don't\s+put\s+a\s+card\s+into\s+your\s+hand\s+this\s+way,\s*"
      + @"create\s+a\s+Food\s+token$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.').Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = EffectWrap.Optional(
      new SearchLibraryEffect
      {
        Filter = new ObjectFilter { Subtypes = ["Food"] },
        Count = LiteralQuantity.Of(1),
        Destination = SearchDestination.Hand,
        Revealed = true,
      },
      isOptional: true,
      ifYouDoNot: new CreateTokenEffect
      {
        Player = ObjectReference.You(),
        Count = LiteralQuantity.Of(1),
        Token = TokenDefinition.Food(),
      }
    );
    return true;
  }
}
