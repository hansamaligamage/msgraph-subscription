using DL;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SendEmailTrigger
{
    class HtmlParser
    {
        public async Task ParseHtml(string invoice, string content, TraceWriter log)
        {
            List<InvoiceLine> lines = new List<InvoiceLine>();
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            //var htmlBody = htmlDoc.DocumentNode.SelectSingleNode("//body/table");
            //doc.Load("C:/Users/ham/Desktop/html.html");
            string xpath = "//body/div/table/tbody/tr";
            int count = doc.DocumentNode.SelectNodes(xpath).Count;
            log.Info("COUNT " + count);
            //omitting the header row
            for (int i = 2; i <= count; i++)
            {
                StringBuilder values = new StringBuilder();
                string trPath = "//body/div/table/tbody/tr[" + i + "]";
                int tdCount = doc.DocumentNode.SelectNodes(trPath + "/td").Count;
                string firstTd = doc.DocumentNode.SelectSingleNode(trPath + "/td[1]").InnerText.Replace("\r\n", string.Empty).Replace("&nbsp;", string.Empty);
                if (!string.IsNullOrEmpty(firstTd))
                {
                    for (int j = 1; j <= tdCount; j++)
                    {
                        string node = doc.DocumentNode.SelectSingleNode(trPath + "/td[" + j + "]").InnerText.Replace("\r\n", string.Empty).Replace("&nbsp;", string.Empty);
                        values.Append(node + ',');
                    }
                    string[] list = values.ToString().Split(',');
                    lines.Add(new InvoiceLine { Invoice = invoice, Item = list[0], Qty = !string.IsNullOrEmpty(list[1]) ? Convert.ToInt32(list[1]) : 0, Price = !string.IsNullOrEmpty(list[2]) ? Convert.ToDouble(list[2]) : 0, Amount = Convert.ToDouble(list[3]) });
                }
            }
            if (lines.Count > 0)
            {
                log.Info("LENGTH " + lines.Count);
                await GenerateDocuments(lines, log);
                log.Info("DONE");
            }
        }
        public async Task GenerateDocuments (List<InvoiceLine> lines, TraceWriter log)
        {
            Random random = new Random();
            using (var context = new InvoiceContext())
            {
                await context.Database.EnsureCreatedAsync();
                log.Info("Database is available!");
                foreach(InvoiceLine line in lines)
                {
                    int id = random.Next(10000);
                    context.InvoiceLines.Add(new InvoiceLine { Id = id, Invoice = line.Invoice, Item = line.Item, Amount = line.Amount, Price = line.Price, Qty = line.Qty });
                    var changeId = await context.SaveChangesAsync();
                    log.Info("Invoice lines are added " + changeId);
                }
            }
        }
    }
}
