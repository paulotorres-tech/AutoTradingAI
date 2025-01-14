
using AutoTradingAI.User.Core.Models;

namespace AutoTradingAI.User.Core.Interfaces
{
    public interface IAuthService
    {
        Task<UserRecord> RegisterAsync(string username, string email, string password);
        Task<string> LoginAsync(string email, string password);
    }
}
