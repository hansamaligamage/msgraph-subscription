using System;
using System.Collections.Generic;
using DL;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Primitives;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace MonthEndTrigger
{
    public static class InvoiceSummary
    {
        [FunctionName("InvoiceSummary")]
        //public static void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed att: {DateTime.Now}");
            const string accountSid = ""; //Twilio API accountSid
            const string authToken = ""; //Twilio API auth token
            var summary =  GenerateInvoiceSummary(log);
            TwilioClient.Init(accountSid, authToken);
            var message = MessageResource.Create(
                body: summary,
                from: new Twilio.Types.PhoneNumber(""), //Twilio API PhoneNo
                to: new Twilio.Types.PhoneNumber("") //Your phone no
            );
            log.Info("Message Sent " + summary);
            return new HttpResponseMessage();
        }

        public static string GenerateInvoiceSummary (TraceWriter log)
        {
            StringBuilder msg = new StringBuilder();
            using (var context = new InvoiceContext())
            {
                var result = from line in context.InvoiceLines
                             group line by line.Invoice into grp
                             select new { Invoice = grp, Total = grp.Sum(s => s.Price) };

                foreach (var r in result)
                    msg.Append(r.Invoice.Key + " : " + r.Total + " | ");
            }
            log.Info("Summary " + msg.ToString());
            return msg.ToString();
        }

    }
}
