using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities
{
    public class ActualOrder : BaseEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ItemCatalogueId { get; set; }  // FK
        public string Month { get; set; }
        public string Year { get; set; }
        public int Quantity { get; set; }
        public string FileName { get; set; }

        [ForeignKey(nameof(ItemCatalogueId))]
        public ItemCatalogue ItemCatalogue { get; set; }

    }

    public class InvalidOrder
    {
        public string Casin { get; set; }
        public string Quantity { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
        public string FileName { get; set; }
    }

    public class OrderFileResponse
    {
        public DataTable DataTable { get; set; }
        public List<ActualOrder> ValidOrders { get; set; }
        public List<InvalidOrder> MissingOrders { get; set; }


        public int ValidOrderCount { get; set; }
        public int InvalidOrderCount { get; set; }
        public int TotalOrderCount { get; set; }
    }

    public class StockFileResponse
    {
        public DataTable DataTable { get; set; }
        public List<InitialStock> ValidStocks { get; set; }
        public List<InvalidStock> MissingStocks { get; set; }
    }


}
