namespace Cirreum.Persistence.Internal.Logging;

using Microsoft.Extensions.Logging;

internal static class EventIds {

	//
	//20_000 - 20_100 Debug Events
	//
	internal const int CosmosItemCreatedId = 20_001;
	public static readonly EventId CosmosItemCreated = new(
		CosmosItemCreatedId,
		nameof(CosmosItemCreated));

	internal const int CosmosItemReadId = 20_002;
	public static readonly EventId CosmosItemRead = new(
		CosmosItemReadId,
		nameof(CosmosItemRead));

	internal const int CosmosItemUpdatedId = 20_003;
	public static readonly EventId CosmosItemUpdated = new(
		CosmosItemUpdatedId,
		nameof(CosmosItemUpdated));

	internal const int CosmosItemSoftDeletedId = 20_004;
	public static readonly EventId CosmosItemSoftDeleted = new(
		CosmosItemSoftDeletedId,
		nameof(CosmosItemSoftDeleted));

	internal const int CosmosItemDeletedId = 20_005;
	public static readonly EventId CosmosItemDeleted = new(
		CosmosItemDeletedId,
		nameof(CosmosItemDeleted));

	internal const int CosmosPointReadStartedId = 20_006;
	public static readonly EventId CosmosPointReadStarted = new(
		CosmosPointReadStartedId,
		nameof(CosmosPointReadStarted));

	internal const int CosmosPointReadExecutedId = 20_007;
	public static readonly EventId CosmosPointReadExecuted = new(
		CosmosPointReadExecutedId,
		nameof(CosmosPointReadExecuted));

	internal const int CosmosQueryConstructedId = 20_008;
	public static readonly EventId CosmosQueryConstructed = new(
		CosmosQueryConstructedId,
		nameof(CosmosQueryConstructed));

	internal const int CosmosQueryExecutedId = 20_009;
	public static readonly EventId CosmosQueryExecuted = new(
		CosmosQueryExecutedId,
		nameof(CosmosQueryExecuted));

	internal const int CosmosQueryDiagnosticsId = 20_099;
	public static readonly EventId CosmosQueryDiagnostics = new(
		CosmosQueryDiagnosticsId,
		nameof(CosmosQueryDiagnostics));


	//
	//20_101 - 20_200 Info Events
	//
	internal const int CosmosInnerRepositoryScopeOpenedId = 20_101;
	public static readonly EventId CosmosInnerRepositoryScopeOpened = new(
		CosmosInnerRepositoryScopeOpenedId,
		nameof(CosmosInnerRepositoryScopeOpened));


	//
	//20_201 - 20_300 Warning Events
	//
	internal const int CosmosItemSoftDeleteNotSupportedId = 20_201;
	public static readonly EventId CosmosItemSoftDeleteNotSupported = new(
		CosmosItemSoftDeleteNotSupportedId,
		nameof(CosmosItemSoftDeleteNotSupported));


	//
	//20_301 - 20_400 Error Events
	//
	// 20_301 no longer belongs to this band by severity — it is emitted at Debug, because both of its
	// call sites raise it for an ordinary point-read miss rather than a fault. The number stays here
	// anyway: consumers filter their logs on the id, and renumbering would break those filters to
	// tidy up a comment. See Log.PointReadMiss.
	internal const int CosmosPointReadMissId = 20_301;
	public static readonly EventId CosmosPointReadMiss = new(
		CosmosPointReadMissId,
		nameof(CosmosPointReadMiss));

}