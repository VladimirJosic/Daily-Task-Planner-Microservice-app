using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserService.Common.DTOs
{
    public class RefreshTokenRequestDto
    {
        public int UserId { get; set; } 
        public string RefreshToken { get; set; }
    }
}
