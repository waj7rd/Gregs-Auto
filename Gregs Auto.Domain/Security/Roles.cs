namespace Gregs_Auto.Domain.Security;

// The staff role vocabulary. These strings go into the Role column and into the
// role claim, so they have to agree with Users.Role in the database.
public static class Roles
{
    // Runs the shop. Everything, including staff accounts.
    public const string Admin = "Admin";

    // Everything except managing staff accounts.
    public const string Manager = "Manager";

    // Works the jobs: sees the schedule, starts and completes appointments.
    // Can't edit or delete customer records.
    public const string Technician = "Technician";

    public static readonly string[] All = [Admin, Manager, Technician];
}

// Named authorization policies, referenced from [Authorize(Policy = ...)].
// Policies rather than bare [Authorize(Roles = "...")] so the rule lives in one
// place and reads as an intent ("who may delete a customer") rather than a list
// of role names scattered across controllers.
public static class Policies
{
    // Add, edit, deactivate staff accounts. Admin only.
    public const string ManageStaff = "ManageStaff";

    // Create, edit, or delete customer and vehicle records.
    public const string ManageCustomers = "ManageCustomers";

    // View customer records without changing them.
    public const string ViewCustomers = "ViewCustomers";

    // Move appointments through their statuses.
    public const string ManageAppointments = "ManageAppointments";
}
