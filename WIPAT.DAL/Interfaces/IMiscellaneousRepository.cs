using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;

namespace WIPAT.DAL.Interfaces
{
    public interface IMiscellaneousRepository
    {
        Task<bool> AddMiscellaneousAsin(List<Miscellaneous> miscItems);
    }
}
