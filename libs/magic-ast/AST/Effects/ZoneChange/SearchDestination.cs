namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Where cards go after searching.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SearchDestination
{
  Hand,
  Battlefield,
  BattlefieldTapped,
  TopOfLibrary,
  Graveyard,
}
