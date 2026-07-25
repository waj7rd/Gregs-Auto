using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.Implementations.Interfaces;
using Gregs_Auto.Domain.IRepositories;

namespace Gregs_Auto.Domain.Implementations;

public class UserLogic : IUserLogic
{
    private readonly IUserRepository _userRepository;

    public UserLogic(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await _userRepository.GetAsync(u => u.Email == email);
        if (user == null)
            return null;

        return PasswordHasher.Verify(password, user.PasswordHash) ? user : null;
    }
}
