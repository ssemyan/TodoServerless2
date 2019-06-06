
# Azure Functions Todo List Sample

This sample demonstrates a simple single page application web frontend and Azure Functions 2.0 api backend. It uses CosmosDB to store data and Azure Active Directory for authentication.

This code can be run locally (using the Azure Functions CLI and CosmosDB emulator) as well as in Azure. Instructions for both are below.

The application is a simple Todo list where users can add items "todo". The items are stored in a single CosmosDB document collection but each user can only access their items (user identification is via the claims from the authentication mechanism). 

The SPA is very simple with Bootstrap for styles, Knockout.js for data binding, and JQuery for ajax calls. 

Users can add new items to their list, or mark existing items as complete (which deletes them). The inital call to the API pulls the current list of items for the user, along with the user's display name (from the auth claims). 

## Setup steps on Localhost

1. Install the Azure CLI tools from here: https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local

1. If you want to use the emulator for local development, install the CosmosDB emulator from here: https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator

1. In the emulator or in Azure, create a new document collection called 'TodoItems' in a new database called 'ServerlessTodo' and set the Partition Key to '/ItemOwner'

1. Update the connection string in **_local.settings.json_** to the one for the emulator or Azure

1. Right click the solution, choose properties, and set both the ui and api project to start. 

## Setup steps on Azure

1. Create a new Azure App Service 

1. Create a new Azure Functions app

1. Create a CosmosDB account

1. Create a new document collection called 'TodoItems' in a new database called 'ServerlessTodo' and set the Partition Key to '/ItemOwner'

1. Copy the connetions string for the CosmosDB account and paste it into a new application setting in the function app called 'CosmosDBConnectionString'

1. Add a CORS setting in the Azure Functions app to allow origins from the App Service

1. Update the remoteUrl locations in **_vars.js_** to point to the functions endpoint

1. Follow the steps here to enable the AAD User Impersonation: https://docs.microsoft.com/en-us/azure/app-service/app-service-web-tutorial-auth-aad#enable-authentication-and-authorization-for-back-end-app

1. Push the code for the front and backend and run. 