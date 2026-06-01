namespace DistributedTransactionCoordinator.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// Domain events are raised within the domain and dispatched after the transaction commits.
/// </summary>
public interface IDomainEvent { }
