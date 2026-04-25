using System;
using System.Linq;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL
{
    public class UserRepository : IUserRepository
    {
        private readonly WIPATContext _context;

        public UserRepository(WIPATContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Response<User> ValidateUser(string username, string password)
        {
            var response = new Response<User>();

            try
            {
                var user = _context.Users.AsNoTracking().FirstOrDefault(u =>
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
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An error occurred during login: {ex.Message}";
            }

            return response;
        }
    }
}