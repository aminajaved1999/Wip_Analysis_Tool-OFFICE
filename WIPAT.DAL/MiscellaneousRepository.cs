using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL
{
    public class MiscellaneousRepository
    {
        public async Task<bool> addMiscellaneousAsin(List<Miscellaneous> MiscItems)
        {
            try
            {
                using (var _context = new WIPATContext())
                {
                    _context.Miscellaneous.AddRange(MiscItems);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch (Exception)
            {
                return false; 
            }
        }

    }
}
