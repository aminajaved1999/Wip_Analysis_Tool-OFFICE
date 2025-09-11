using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities.Enum;

namespace WIPAT.Entities.Dto
{
    public class Response<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public StatusType Status { get; set; }

    }
}
