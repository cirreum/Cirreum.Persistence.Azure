namespace Cirreum.Persistence.Internal;

using System.Collections.Generic;

class ContinuationPage<T>(int? total, int size, IReadOnlyList<T> items, double charge, string? continuation = null)
	: IContinuationPage<T>
	where T : IEntity {

	public int? Total { get; } = total;

	public int Size { get; } = size;

	public IReadOnlyList<T> Entities { get; } = items;

	public double Charge { get; } = charge;

	public string? Continuation { get; } = continuation;

}