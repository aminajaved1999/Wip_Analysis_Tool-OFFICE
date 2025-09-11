using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL
{
    public class UserRepository
    {
        public Response<bool> ValidateUser(string username, string password)
        {
            var response = new Response<bool>();

            try
            {
                using (var _context = new WIPATContext())
                {
                    bool isValidUser = _context.Users.Any(u =>
                        u.UserName == username &&
                        u.Password == password &&
                        u.Status == true
                    );

                    response.Success = true;
                    response.Data = isValidUser;
                    response.Message = isValidUser ? "Login successful." : "Invalid username or password.";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Data = false;
                response.Message = $"An error occurred during login: {ex.Message}";
            }

            return response;
        }


    }
}
