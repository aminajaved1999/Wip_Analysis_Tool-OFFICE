using System;
using System.Collections.Generic;
using System.Data;
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

        public bool IsContinueWithInactiveItems { get; set; }
        public List<string> MissingItems { get; set; } = new List<string>();
        public List<string> DeactivatedItems { get; set; } = new List<string>();
        public DataTable  ProblemItemsTable { get; set; } = new DataTable();

    }
}
