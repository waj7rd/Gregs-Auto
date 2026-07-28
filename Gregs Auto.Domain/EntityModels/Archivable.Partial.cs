namespace Gregs_Auto.Domain.EntityModels;

// Hand-written halves of the scaffolded entities, kept separate so regenerating
// them doesn't drop these.
//
// None of these three can be deleted once used: a Service is referenced by every
// appointment booked against it, and a Customer's Vehicle carries the service
// history. Archiving hides them from day-to-day lists while leaving history
// intact and resolvable.

public partial class Service
{
    public bool IsActive { get; set; } = true;
}

public partial class Customer
{
    public bool IsActive { get; set; } = true;
}

public partial class Vehicle
{
    public bool IsActive { get; set; } = true;
}
