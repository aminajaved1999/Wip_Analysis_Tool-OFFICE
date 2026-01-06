using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL
{
    public class UserRepository
    {
        public Response<User> ValidateUser(string username, string password)
        {
            var response = new Response<User>();

            try
            {
                using (var _context = new WIPATContext())
                {
                    var user = _context.Users.FirstOrDefault(u =>
                        u.UserName == username &&
                        u.Password == password &&
                        u.Status == true
                    );

                    if (user != null)
                    {
                        response.Success = true;
                        response.Data = user;
                        response.Message = "Login successful.";
                    }
                    else
                    {
                        response.Success = false;
                        response.Data = null;
                        response.Message = "Invalid username or password.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An error occurred during login: {ex.Message}";
            }

            return response;
        }


    }
}
