using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserService.Data.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public required string Token { get; set; }
        public int UserId { get; set; }
        public DateTime ExpiresOnUtc { get; set; }
        public User User { get; set; }
    }
}
