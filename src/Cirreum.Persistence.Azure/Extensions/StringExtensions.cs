namespace Cirreum;

internal static class StringExtensions {

	/// <summary>
	/// Converts the first character of a PascalCase string to lowercase (camelCase).
	/// </summary>
	/// <example>"ModifiedBy" → "modifiedBy"</example>
	public static string Camelize(this string value) {

		if (string.IsNullOrWhiteSpace(value)) {
			return value;
		}

		Span<char> buffer = stackalloc char[value.Length];
		var index = 0;
		var capitalize = false;

		for (var i = 0; i < value.Length; i++) {
			var c = value[i];

			if (c == '_' || c == '-' || c == ' ') {
				capitalize = true;
				continue;
			}

			if (index == 0) {
				buffer[index++] = char.ToLowerInvariant(c);
				continue;
			}

			if (capitalize) {
				buffer[index++] = char.ToUpperInvariant(c);
				capitalize = false;
			} else {
				buffer[index++] = c;
			}
		}

		return new string(buffer[..index]);
	}

}