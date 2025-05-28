using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos;
using api.Dtos.Users;
using api.Models;

namespace api.Mappers
{
    public static class UsersMappers
    {
        public static UsersDtos ToUserDtos(this Users userModel)
        {
            return new UsersDtos
            {
                ID = userModel.ID,
                Email = userModel.Email,
                Name = userModel.Name,
                Password = userModel.Password,
                Rolebase = userModel.Rolebase,
            };
         
        
        }
       
       public static Users ToCreateUserDtos(this CreateUserDtos userDtos)
        {
            return new Users
            {
                Email = userDtos.Email,
                Name = userDtos.Name,
                Password = userDtos.Password,
            };
        }       
    
    }


}