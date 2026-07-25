using Gregs_Auto.Domain.EntityModels;

namespace Gregs_Auto.Domain.Implementations.Interfaces;

// Business-logic contract for staff accounts.
public interface IUserLogic
{
    // Looks up the user by email and verifies the password. Returns null on
    // any failure (unknown email, wrong password) — the caller doesn't need
    // to know which, so the login form can show one generic error.
    Task<User?> AuthenticateAsync(string email, string password);
}
