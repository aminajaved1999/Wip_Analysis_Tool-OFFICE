using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;

namespace WIPAT.DAL
{
    public class MiscellaneousRepository : IMiscellaneousRepository
    {
        private readonly WIPATContext _context;

        public MiscellaneousRepository(WIPATContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<bool> AddMiscellaneousAsin(List<Miscellaneous> miscItems)
        {
            try
            {
                _context.Miscellaneous.AddRange(miscItems);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}