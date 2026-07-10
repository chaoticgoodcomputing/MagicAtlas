namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "exile target permanent card from your graveyard." — a single-target exile
/// of a card sitting in the controller's graveyard, restricted to permanent
/// card types (CR 110.4a: a "permanent card" is a card that is a permanent
/// card type — artifact, battle, creature, enchantment, land, or planeswalker).
///
/// <para>
/// Distinct from <see cref="ExileTargetTriggeredRule"/> (which handles bare
/// "exile target [filter]" with no zone suffix, via
/// <see cref="MagicAST.Parsing.Parsers.Spell.SpellRuleHelpers.ParseTargetFilter"/>,
/// which does not parse a trailing "from your graveyard" zone phrase): this
/// rule is anchored to the exact "... permanent card from your graveyard"
/// composite surface (Savvy Trader's ETB), so it does not widen the shared
/// helper's grammar or collide with the bare-filter sibling.
/// </para>
///
/// CR 701.13a (exile); CR 110.4a (permanent card); CR 404 (graveyard).
/// </summary>
[TriggeredRule(Priority = 90)]
public sealed class ExileTargetPermanentCardFromGraveyardTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^exile\s+target\s+permanent\s+card\s+from\s+your\s+graveyard$",
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

    effect = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["permanent"],
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
    };
    return true;
  }
}
