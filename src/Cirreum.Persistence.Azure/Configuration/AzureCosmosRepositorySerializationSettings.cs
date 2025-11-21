namespace Cirreum.Persistence.Configuration;

using System.Text.Json;

public sealed class AzureCosmosRepositorySerializationSettings {

	/// <summary>
	/// Gets or sets if the serializer should use indentation.
	/// </summary>
	/// <remarks>The default value is false</remarks>
	public bool Indented { get; set; }

	/// <summary>
	/// Gets or sets the naming policy used to convert a string-based name to
	/// another format, such as a camel-casing format.
	/// </summary>
	/// <remarks>The default value is <c>CamelCase</c>.</remarks>
	public string NamingPolicy { get; set; } = "CamelCase";

	/// <summary>
	/// Gets the <see cref="JsonNamingPolicy"/> from the <see cref="NamingPolicy"/> value.
	/// </summary>
	/// <remarks>The default value is <see cref="JsonNamingPolicy.CamelCase"/>.</remarks>
	public JsonNamingPolicy MappedNamingPolicy {
		get {
			return this.NamingPolicy switch {
				nameof(JsonNamingPolicy.KebabCaseUpper) => JsonNamingPolicy.KebabCaseUpper,
				nameof(JsonNamingPolicy.KebabCaseLower) => JsonNamingPolicy.KebabCaseLower,
				nameof(JsonNamingPolicy.SnakeCaseLower) => JsonNamingPolicy.SnakeCaseLower,
				nameof(JsonNamingPolicy.SnakeCaseUpper) => JsonNamingPolicy.SnakeCaseUpper,
				_ => JsonNamingPolicy.CamelCase
			};
		}
	}

}