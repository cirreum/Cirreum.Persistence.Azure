namespace Cirreum.Persistence.Internal.Logging;

using Microsoft.Extensions.Logging;
using System.Linq;
using System.Text.Json;

internal static class LoggerExtensions {

	//Debug Level Extensions
	public static void LogItemCreated<TEntity>(this ILogger logger, TEntity item) where TEntity : IEntity =>
		logger.ItemCreated(JsonSerializer.Serialize(item), null);

	public static void LogItemRead<TEntity>(this ILogger logger, TEntity item) where TEntity : IEntity =>
		logger.ItemRead(JsonSerializer.Serialize(item), null);

	public static void LogItemUpdated<TEntity>(this ILogger logger, TEntity item) where TEntity : IEntity =>
		logger.ItemUpdated(JsonSerializer.Serialize(item), null);

	public static void LogItemSoftDeleted<TEntity>(this ILogger logger, string id) where TEntity : IEntity =>
		logger.ItemSoftDeleted(id, null);

	public static void LogItemDeleted<TEntity>(this ILogger logger, string id) where TEntity : IEntity =>
		logger.ItemDeleted(id, null);

	public static void LogQueryDiagnostics<TEntity>(this ILogger logger, string diagnostics) where TEntity : IEntity =>
		logger.QueryDiagnostics(diagnostics, null);

	public static void LogPointReadStarted<TEntity>(this ILogger logger, string id, string partitionKey) where TEntity : IEntity =>
		logger.PointReadStarted(typeof(TEntity).Name, id, partitionKey, null);

	public static void LogPointReadExecuted<TEntity>(this ILogger logger, double ruCharge) where TEntity : IEntity =>
		logger.PointReadExecuted(typeof(TEntity).Name, ruCharge, null);

	public static void LogQueryConstructed<TEntity>(this ILogger logger, IQueryable<TEntity> queryable) where TEntity : IEntity =>
		logger.QueryConstructed(typeof(TEntity).Name, queryable.ToString() ?? "", null);
	public static void LogQueryConstructed<TEntity>(this ILogger logger, string query) where TEntity : IEntity =>
		logger.QueryConstructed(typeof(TEntity).Name, query ?? "", null);

	public static void LogQueryExecuted<TEntity>(this ILogger logger, IQueryable<TEntity> queryable, double charge) where TEntity : IEntity =>
		logger.QueryExecuted(typeof(TEntity).Name, charge, queryable.ToString() ?? "", null);

	public static void LogQueryExecuted<TEntity>(this ILogger logger, string query, double charge) where TEntity : IEntity =>
		logger.QueryExecuted(typeof(TEntity).Name, charge, query ?? "", null);


	// Infos


	// Warnings
	public static void LogItemSoftDeleteNotSupported<TEntity>(this ILogger logger, string id) where TEntity : IEntity =>
		logger.ItemSoftDeleteNotSupported(id, null);



	// Errors
	public static void LogPointReadException<TEntity>(this ILogger logger, double ruCharge, Exception exception) where TEntity : IEntity =>
		logger.PointReadException(typeof(TEntity).Name, ruCharge, exception);


}