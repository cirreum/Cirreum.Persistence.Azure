namespace Cirreum.Persistence.Configuration;

using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;

/// <summary>
/// A curated, configuration-bindable subset of <see cref="CosmosClientOptions"/>.
/// </summary>
/// <remarks>
/// Only the options that are safe and meaningful to set from application settings are
/// exposed here; options managed by the provider (Serializer, HttpClientFactory,
/// ApplicationName, EnableContentResponseOnWrite, AllowBulkExecution) are not.
/// Every property is optional — only explicitly configured values are applied, and the
/// <c>configureClientOptions</c> callback on <c>AddCosmosDb(...)</c> runs afterwards,
/// so code-level configuration always wins over configuration-bound values.
/// </remarks>
public sealed class AzureCosmosClientSettings {

	/// <summary>
	/// Gets or sets the connection mode. The SDK defaults to <see cref="ConnectionMode.Direct"/>;
	/// use <see cref="ConnectionMode.Gateway"/> for gateway-only environments such as the
	/// Linux-based Cosmos DB emulator (vnext) or networks that block the direct TCP port range.
	/// </summary>
	public ConnectionMode? ConnectionMode { get; set; }

	/// <summary>
	/// Gets or sets whether the client should limit connections to the configured endpoint,
	/// skipping account-level endpoint discovery. Useful for emulators and single-region accounts.
	/// </summary>
	public bool? LimitToEndpoint { get; set; }

	/// <summary>
	/// Gets or sets the consistency level override. When unset, the account default applies.
	/// </summary>
	public ConsistencyLevel? ConsistencyLevel { get; set; }

	/// <summary>
	/// Gets or sets the preferred region for the client, e.g. "East US".
	/// </summary>
	public string? ApplicationRegion { get; set; }

	/// <summary>
	/// Gets or sets the ordered list of preferred regions for the client.
	/// Takes precedence over <see cref="ApplicationRegion"/> when both are set.
	/// </summary>
	public List<string>? ApplicationPreferredRegions { get; set; }

	/// <summary>
	/// Gets or sets the request timeout.
	/// </summary>
	public TimeSpan? RequestTimeout { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of retries on rate-limited (429) requests.
	/// </summary>
	public int? MaxRetryAttemptsOnRateLimitedRequests { get; set; }

	/// <summary>
	/// Gets or sets the maximum retry wait time on rate-limited (429) requests.
	/// </summary>
	public TimeSpan? MaxRetryWaitTimeOnRateLimitedRequests { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of concurrent connections in Gateway mode.
	/// Only applies when <see cref="ConnectionMode"/> is <see cref="ConnectionMode.Gateway"/>.
	/// </summary>
	public int? GatewayModeMaxConnectionLimit { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of concurrent requests per TCP connection (Direct mode).
	/// </summary>
	public int? MaxRequestsPerTcpConnection { get; set; }

	/// <summary>
	/// Gets or sets the maximum number of TCP connections per backend endpoint (Direct mode).
	/// </summary>
	public int? MaxTcpConnectionsPerEndpoint { get; set; }

	/// <summary>
	/// Gets or sets the idle TCP connection timeout (Direct mode).
	/// </summary>
	public TimeSpan? IdleTcpConnectionTimeout { get; set; }

	/// <summary>
	/// Gets or sets the port reuse policy for Direct mode connections.
	/// </summary>
	public PortReuseMode? PortReuseMode { get; set; }

	/// <summary>
	/// Gets or sets whether the client detects and recovers from connection endpoint changes (Direct mode).
	/// </summary>
	public bool? EnableTcpConnectionEndpointRediscovery { get; set; }

	/// <summary>
	/// Applies every explicitly configured value onto the provided <paramref name="options"/>.
	/// Unset (null) values leave the corresponding SDK option untouched.
	/// </summary>
	internal void ApplyTo(CosmosClientOptions options) {

		if (this.ConnectionMode is { } connectionMode) {
			options.ConnectionMode = connectionMode;
		}
		if (this.LimitToEndpoint is { } limitToEndpoint) {
			options.LimitToEndpoint = limitToEndpoint;
		}
		if (this.ConsistencyLevel is { } consistencyLevel) {
			options.ConsistencyLevel = consistencyLevel;
		}
		if (this.ApplicationRegion is { Length: > 0 } applicationRegion) {
			options.ApplicationRegion = applicationRegion;
		}
		if (this.ApplicationPreferredRegions is { Count: > 0 } preferredRegions) {
			options.ApplicationPreferredRegions = preferredRegions;
		}
		if (this.RequestTimeout is { } requestTimeout) {
			options.RequestTimeout = requestTimeout;
		}
		if (this.MaxRetryAttemptsOnRateLimitedRequests is { } maxRetryAttempts) {
			options.MaxRetryAttemptsOnRateLimitedRequests = maxRetryAttempts;
		}
		if (this.MaxRetryWaitTimeOnRateLimitedRequests is { } maxRetryWaitTime) {
			options.MaxRetryWaitTimeOnRateLimitedRequests = maxRetryWaitTime;
		}
		if (this.GatewayModeMaxConnectionLimit is { } gatewayMaxConnections) {
			options.GatewayModeMaxConnectionLimit = gatewayMaxConnections;
		}
		if (this.MaxRequestsPerTcpConnection is { } maxRequestsPerConnection) {
			options.MaxRequestsPerTcpConnection = maxRequestsPerConnection;
		}
		if (this.MaxTcpConnectionsPerEndpoint is { } maxConnectionsPerEndpoint) {
			options.MaxTcpConnectionsPerEndpoint = maxConnectionsPerEndpoint;
		}
		if (this.IdleTcpConnectionTimeout is { } idleTimeout) {
			options.IdleTcpConnectionTimeout = idleTimeout;
		}
		if (this.PortReuseMode is { } portReuseMode) {
			options.PortReuseMode = portReuseMode;
		}
		if (this.EnableTcpConnectionEndpointRediscovery is { } endpointRediscovery) {
			options.EnableTcpConnectionEndpointRediscovery = endpointRediscovery;
		}

	}

}
