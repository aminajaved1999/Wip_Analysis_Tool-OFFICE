using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL.Interfaces
{
    public interface IUserRepository
    {
        Response<User> ValidateUser(string username, string password);
    }
}
