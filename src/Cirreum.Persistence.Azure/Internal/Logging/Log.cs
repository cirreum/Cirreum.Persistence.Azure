namespace Cirreum.Persistence.Internal.Logging;

using Microsoft.Extensions.Logging;

internal static partial class Log {

	//
	// Debug
	//
	[LoggerMessage(
		EventId = EventIds.CosmosItemCreatedId,
		Level = LogLevel.Debug,
		Message = "Cosmos item created: {CosmosItemJson}")]
	public static partial void ItemCreated(this ILogger logger,
		string CosmosItemJson, Exception? exception);

	[LoggerMessage(
		EventId = EventIds.CosmosItemReadId,
		Level = LogLevel.Debug,
		Message = "Cosmos item read: {CosmosItemJson}")]
	public static partial void ItemRead(this ILogger logger,
		string CosmosItemJson, Exception? exception);

	[LoggerMessage(
		EventId = EventIds.CosmosItemUpdatedId,
		Level = LogLevel.Debug,
		Message = "Cosmos item updated: {CosmosItemJson}")]
	public static partial void ItemUpdated(this ILogger logger,
		string CosmosItemJson, Exception? exception);

	[LoggerMessage(
		EventId = EventIds.CosmosItemSoftDeletedId,
		Level = LogLevel.Debug,
		Message = "Cosmos item soft-deleted: {Id}")]
	public static partial void ItemSoftDeleted(this ILogger logger,
		string Id, Exception? exception);

	[LoggerMessage(
		EventId = EventIds.CosmosItemDeletedId,
		Level = LogLevel.Debug,
		Message = "Cosmos item deleted: {Id}")]
	public static partial void ItemDeleted(this ILogger logger,
		string Id, Exception? exception);

	[LoggerMessage(
		EventId = EventIds.CosmosQueryDiagnosticsId,
		Level = LogLevel.Debug,
		Message = "Cosmos query diagnostics: {Diagnostics}")]
	public static partial void QueryDiagnostics(this ILogger logger,
		string Diagnostics, Exception? exception);

	[LoggerMessage(
		EventId = EventIds.CosmosPointReadStartedId,
		Level = LogLevel.Debug,
		Message = "Point read started for item type {CosmosItemType} with id {CosmosItemId} and partitionKey {CosmosItemPartitionKey}")]
	public static partial void PointReadStarted(this ILogger logger,
		string CosmosItemType, string CosmosItemId, string CosmosItemPartitionKey, Exception? exception);

	[LoggerMessage(
		EventId = EventIds.CosmosPointReadExecutedId,
		Level = LogLevel.Debug,
		Message = "Point read executed for item type {CosmosItemType} total RU cost {CosmosOperationRUCharge}")]
	public static partial void PointReadExecuted(this ILogger logger,
		string CosmosItemType, double CosmosOperationRUCharge, Exception? exception);

	[LoggerMessage(
		EventId = EventIds.CosmosQueryConstructedId,
		Level = LogLevel.Debug,
		Message = "Cosmos query constructed for item type {CosmosItemType}: {CosmosQuery}")]
	public static partial void QueryConstructed(this ILogger logger,
		string CosmosItemType, string CosmosQuery, Exception? exception);

	[LoggerMessage(
		EventId = EventIds.CosmosQueryExecutedId,
		Level = LogLevel.Debug,
		Message = "Cosmos query executed for item type {CosmosItemType} with a charge of {CosmosOperationRUCharge} RUs Query: {CosmosQuery}")]
	public static partial void QueryExecuted(this ILogger logger,
		string CosmosItemType, double CosmosOperationRUCharge, string CosmosQuery, Exception? exception);



	// Warnings

	[LoggerMessage(
		EventId = EventIds.CosmosItemSoftDeleteNotSupportedId,
		Level = LogLevel.Warning,
		Message = "Cosmos item soft-delete not supported: {Id}")]
	public static partial void ItemSoftDeleteNotSupported(this ILogger logger,
		string Id, Exception? exception);


	// Errors

	[LoggerMessage(
		EventId = EventIds.CosmosPointReadExceptionId,
		Level = LogLevel.Error,
		Message = "Point read encountered an exception for item type {CosmosItemType} total RU cost {CosmosOperationRUCharge}")]
	public static partial void PointReadException(this ILogger logger,
		string CosmosItemType, double CosmosOperationRUCharge, Exception? exception);


}
