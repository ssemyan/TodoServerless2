using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ServerlessTodoApi
{
    // How to enable AAD user impersonation: https://docs.microsoft.com/en-us/azure/app-service/app-service-web-tutorial-auth-aad#enable-authentication-and-authorization-for-back-end-app

    public static class TodoItems
	{
		private static AuthorizedUser GetCurrentUserName(ILogger log, ClaimsPrincipal principal)
		{
			// On localhost claims will be empty
			string name = "Dev User";
			string upn = "dev@localhost";

            if (principal != null)
            {
                foreach (Claim claim in principal.Claims)
                {
                    if (claim.Type == "name")
                    {
                        name = claim.Value;
                    }
                    if (claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
                    {
                        upn = claim.Value;
                    }
                    //Uncomment to print all claims to log output for debugging
                    //log.LogInformation("Claim: " + claim.Type + " Value: " + claim.Value);
                }
            }
            else
            {
                log.LogInformation("No claims information - assuming localhost dev user");
            }
			return new AuthorizedUser() {DisplayName = name, UniqueName = upn };
		}
		
		// Add new item
		[FunctionName("TodoItemAdd")]
		public static HttpResponseMessage AddItem(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todoitem")]HttpRequestMessage req,
            [CosmosDB("ServerlessTodo", "TodoItems", ConnectionStringSetting = "CosmosDBConnectionString")] out TodoItem newTodoItem,
            ILogger log,
            ClaimsPrincipal principal)
		{
			// Get request body
			TodoItem newItem = req.Content.ReadAsAsync<TodoItem>().Result;
			log.LogInformation("Upserting item: " + newItem.ItemName);
			if (string.IsNullOrEmpty(newItem.id))
			{
				// New Item so add ID and date
				log.LogInformation("Item is new.");
				newItem.id = Guid.NewGuid().ToString();
				newItem.ItemCreateDate = DateTime.Now;
				newItem.ItemOwner = GetCurrentUserName(log, principal).UniqueName;
			}
			newTodoItem = newItem;

			return req.CreateResponse(HttpStatusCode.OK, newItem);
		}
        
		// Get all items
		[FunctionName("TodoItemGetAll")]
		public static HttpResponseMessage GetAll(
		   [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todoitem")]HttpRequestMessage req,
		   [CosmosDB("ServerlessTodo", "TodoItems", ConnectionStringSetting = "CosmosDBConnectionString", CreateIfNotExists = true)] DocumentClient client,
           ILogger log,
           ClaimsPrincipal principal)
		{
			var currentUser = GetCurrentUserName(log, principal);
			log.LogInformation("Getting all Todo items for user: " + currentUser.UniqueName);

			Uri collectionUri = UriFactory.CreateDocumentCollectionUri("ServerlessTodo", "TodoItems");

			var itemQuery = client.CreateDocumentQuery<TodoItem>(collectionUri, new FeedOptions { PartitionKey = new PartitionKey(currentUser.UniqueName) });

			var ret = new { UserName = currentUser.DisplayName, Items = itemQuery.ToArray() };

			return req.CreateResponse(HttpStatusCode.OK, ret);
		}
        
		// Delete item by id
		[FunctionName("TodoItemDelete")]
		public static async Task<HttpResponseMessage> DeleteItem(
		   [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todoitem/{id}")]HttpRequestMessage req,
		   [CosmosDB("ServerlessTodo", "TodoItems", ConnectionStringSetting = "CosmosDBConnectionString")] DocumentClient client, string id,
           ILogger log,
           ClaimsPrincipal principal)
		{
			var currentUser = GetCurrentUserName(log, principal);
			log.LogInformation("Deleting document with ID " + id + " for user " + currentUser.UniqueName);

			Uri documentUri = UriFactory.CreateDocumentUri("ServerlessTodo", "TodoItems", id);

			try
			{
				// Verify the user owns the document and can delete it
    			await client.DeleteDocumentAsync(documentUri, new RequestOptions() { PartitionKey = new PartitionKey(currentUser.UniqueName) });
			}
			catch (DocumentClientException ex)
			{
				if (ex.StatusCode == HttpStatusCode.NotFound)
				{
					// Document does not exist, is not owned by the current user, or was already deleted
					log.LogInformation("Document with ID: " + id + " not found.");
				}
				else
				{
					// Something else happened
					throw ex;
				}
			}

			return req.CreateResponse(HttpStatusCode.NoContent);
		}
	}
}
