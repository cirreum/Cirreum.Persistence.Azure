namespace Cirreum.Persistence.Internal;

using Cirreum.Persistence.Configuration;
using System.Collections.Concurrent;

internal static class InstanceSettingsRegistry {

	static readonly ConcurrentDictionary<string, AzureCosmosInstanceSettings> keyedSettings = [];

	public static void RegisterKeyedSettings(string connectionKey, AzureCosmosInstanceSettings settings) {
		keyedSettings.TryAdd(connectionKey, settings);
	}

	public static AzureCosmosInstanceSettings GetSettings(string key) {
		if (keyedSettings.TryGetValue(key, out var settings)) {
			return settings;
		}
		throw new InvalidOperationException($"Settings for Key '{key}' not found.");
	}

}