namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Types of partner abilities.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PartnerType
{
  /// <summary>Generic partner (can partner with any other Partner commander)</summary>
  Partner,

  /// <summary>Partner with a specific named card</summary>
  PartnerWith,

  /// <summary>Choose a Background (partners with Background enchantments)</summary>
  ChooseABackground,

  /// <summary>Friends forever (partners with other Friends forever commanders)</summary>
  FriendsForever,

  /// <summary>Doctor's companion (partners with Doctor cards)</summary>
  DoctorsCompanion,
}
