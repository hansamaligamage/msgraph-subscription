using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DL
{
    public class InvoiceContext : DbContext
    {
        public DbSet<InvoiceLine> InvoiceLines { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseCosmosSql( "service endpoint", "authkey", "database");
        }
    }
}
