# msgraph-subscription
Subscribe to Outlook email box with invoices and save invoice lines in a CosmosDB, at the end of the month generate an invoice summary and send a twilio sms to finance manager or relevant authority using two azure functions

Used Visual Studio 2017 with .NET Core 2.0 version to develop the solution, You can go through this article and find about more details on Graph API, <a href="https://social.technet.microsoft.com/wiki/contents/articles/51599.net-core-building-function-app-with-microsoft-graph-api-and-azure-functions.aspx">.NET  Core: Building Function app with Microsoft Graph API and Azure Functions</a>

Subscribe to mail box and track changes
```
[FunctionName("EmailTrigger")] 
public static async Task Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", 
 Route = null)]HttpRequestMessage req, TraceWriter log) 
{ 
  log.Info("C# HTTP trigger function processed a request."); 
  string validationToken; 
  if (GetValidationToken(req, out validationToken)) 
  { 
    return PlainTextResponse(validationToken); 
  } 
  //Process each notification 
  var response = await ProcessWebhookNotificationsAsync(req, log, async hook => 
  { 
   return await CheckForSubscriptionChangesAsync(hook.Resource, log); 
  }); 
  return response; 
}
```

Process email box chanegs, email notification is parsed into an object for later use
```
private static async Task ProcessWebhookNotificationsAsync(HttpRequestMessage req, TraceWriter log,
Func> processSubscriptionNotification) 
{ 
  // Read the body of the request and parse the notification 
  string content = await req.Content.ReadAsStringAsync(); 
  log.Verbose($"Raw request content: {content}"); 
  var webhooks = JsonConvert.DeserializeObject(content); 
  if (webhooks?.Notifications != null) 
  { 
    // Since webhooks can be batched together, loop over all the notifications we receive and process them
    separately. 
     foreach (var hook in webhooks.Notifications) 
     { 
       log.Info($"Hook received for subscription: '{hook.SubscriptionId}' Resource: '{hook.Resource}', 
       changeType: '{hook.ChangeType}'"); 
       try 
       { 
         await processSubscriptionNotification(hook); 
       } 
       catch (Exception ex) 
       { 
         log.Error($"Error processing subscription notification. Subscription {hook.SubscriptionId} was skipped.
         {ex.Message}", ex); 
        } 
     } 
   // After we process all the messages, return an empty response. 
   return req.CreateResponse(HttpStatusCode.NoContent); 
  } 
  else 
  { 
    log.Info($"Request was incorrect. Returning bad request."); 
    return req.CreateResponse(HttpStatusCode.BadRequest); 
  } 
}
```

Extract the required details in each email, subject & body parameters
```
private static async Task CheckForSubscriptionChangesAsync(string resource, TraceWriter log) 
{ 
 bool success = false; 
 // Obtain an access token 
 string accessToken = System.Environment.GetEnvironmentVariable("AccessToken", EnvironmentVariableTarget.Process); 
 log.Info($"accessToken: {accessToken}"); 
 HttpClient client = new HttpClient(); 
 // Send Graph request to fetch mail 
 HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/" + resource); 
 request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);  
 HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(continueOnCapturedContext: false);  
 log.Info(response.ToString()); 
 if (response.IsSuccessStatusCode) 
 { 
  var result = await response.Content.ReadAsStringAsync(); 
  JObject obj = (JObject)JsonConvert.DeserializeObject(result); 
  string subject = (string)obj["subject"]; 
  log.Verbose($"Subject : {subject}"); 
  string content = (string)obj["body"]["content"]; 
  log.Verbose($"Email Body : {content}"); 
  success = true; 
 } 
 return success; 
}
```

Connect to CosmosDB and create new documents using EFCore CosmosDB API
```
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
            context.InvoiceLines.Add(new InvoiceLine { Id = id, Invoice = line.Invoice, Item = line.Item,
            Amount = line.Amount, Price = line.Price, Qty = line.Qty });
            var changeId = await context.SaveChangesAsync();
            log.Info("Invoice lines are added " + changeId);
        }
    }
```

Retrieve CosmosDB documents 
```
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
```

Send a invoice summary as a twilio message
```
[FunctionName("InvoiceSummary")]
public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post",
Route = null)]HttpRequestMessage req, TraceWriter log)
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
```

