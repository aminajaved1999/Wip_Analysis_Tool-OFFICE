using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.DAL;
using WIPAT.Entities.Dto;

namespace WIPAT.BLL
{
    public class AuthManager
    {
        private UserRepository userRepository;

        public AuthManager()
        {
            userRepository = new UserRepository();
        }
      

    }
}
