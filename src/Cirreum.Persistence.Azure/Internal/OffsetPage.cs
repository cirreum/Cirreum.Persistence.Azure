namespace Cirreum.Persistence.Internal;

using System.Collections.Generic;

sealed class OffsetPage<T>(
	int? total,
	int? pageNumber,
	int size,
	IReadOnlyList<T> items,
	double charge,
	string? continuation = null)
	: ContinuationPage<T>(total, size, items, charge, continuation), IOffSetPage<T>
	where T : IEntity {

	public int? TotalPages => this.GetTotalPages();

	public int? PageNumber { get; } = pageNumber;

	private int? GetTotalPages() {
		if (this.Total.HasValue) {
			return (int)Math.Abs(Math.Ceiling(this.Total.Value / (double)this.Size));
		}
		return null;
	}

}