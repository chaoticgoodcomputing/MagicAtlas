namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Reconfigure [cost]: two activated abilities per CR 702.151a.
///
/// <para>
/// CR 702.151a (verbatim): "Reconfigure represents two activated abilities.
/// Reconfigure [cost] means "[Cost]: Attach this permanent to another target
/// creature you control. Activate only as a sorcery" and "[Cost]: Unattach
/// this permanent. Activate only if this permanent is attached to a creature
/// and only as a sorcery."
/// </para>
///
/// <para>
/// Oracle-text parsing is handled by
/// <see cref="MagicAST.Parsing.Parsers.Static.ReconfigureStaticRule"/> (priority 1001),
/// which returns both activated abilities as a list. This keyword file keeps
/// the combinator live as a fallback but no longer uses the deleted
/// <c>ReconfigureEffect</c> opaque marker. The <see cref="Definition"/> is null
/// because <see cref="IKeywordExpander.Expand"/> can only return a single
/// <see cref="Ability"/> and Reconfigure decomposes into two.
/// </para>
/// </summary>
[Keyword]
public sealed class ReconfigureKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  /// <remarks>
  /// Null: the keyword expander returns a single Ability, but Reconfigure
  /// decomposes into two ActivatedAbility nodes (CR 702.151a). The oracle-text
  /// parser handles the two-ability output via ReconfigureStaticRule.
  /// </remarks>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Reconfigure")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new ActivatedAbility
    {
      KeywordSource = "Reconfigure",
      Costs = [cost],
      Effects =
      [
        new AttachEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Controller = ControllerFilter.You,
            },
          },
        },
      ],
      Restrictions = [ActivationRestriction.OnlyAsSorcery],
      IsManaAbility = false,
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Mana-cost-parameter parser, inlined from the former
  /// <c>KeywordDefinitions.ParseManaCost</c>.
  /// </summary>
  private static ManaCost ParseManaCost(string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter))
    {
      throw new ArgumentException("Reconfigure requires a mana cost parameter.", nameof(parameter));
    }

    var parsed = new ManaCostParser().Parse(parameter.Trim());
    return new ManaCost { Symbols = parsed.Symbols.ToList() };
  }
}
