namespace Cirreum.Persistence;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

sealed partial class InMemoryRepository<TEntity> {

	private static string SerializeItem(
	   TEntity item,
	   string? etag = null,
	   long? ts = null) {
		ArgumentNullException.ThrowIfNull(item);
		var jObject = JObject.FromObject(item);
		if (etag != null) {
			jObject["_etag"] = JToken.FromObject(etag);
		}

		if (ts.HasValue) {
			jObject["_ts"] = JToken.FromObject(ts);
		}

		return jObject.ToString();

	}

	internal TEntity DeserializeItem(string jsonItem) =>
		JsonConvert.DeserializeObject<TEntity>(jsonItem)!;

	internal static TDeserializeTo DeserializeItem<TDeserializeTo>(string jsonItem) =>
		JsonConvert.DeserializeObject<TDeserializeTo>(jsonItem)!;

}