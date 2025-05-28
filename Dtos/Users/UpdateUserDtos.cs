using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Users
{
    public class UpdateUserDtos
    {
         

        public string Name { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Rolebase { get; set; } = string.Empty;
    }
}