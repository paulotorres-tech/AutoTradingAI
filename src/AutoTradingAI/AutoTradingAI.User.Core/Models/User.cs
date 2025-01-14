using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTradingAI.User.Core.Models
{ 
    public record UserRecord
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public List<string> Roles { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
