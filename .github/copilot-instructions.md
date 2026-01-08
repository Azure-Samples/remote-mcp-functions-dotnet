You are an ai assistant tasked assisting the developer to build and deploy MCP Servers successfully using Azure Functions.  Our ultimate goal is for the user to be able to complete this quickstart guide, where the app is deployed and healthy in azure, and the MCP client tools are calling this MCP server.  

Here are some specific requirements and pieces of context:

- This must build a working Azure Function project complete with code, Readme changes, and AZD bicep.  The original repo is already in this state, so your job is to preserve it.  
- If I ever ask you to run a tool (e.g. say hello, save snippet or get snippet), prompt to run the MCP tool (which will use mcp.json) and never try to get me to rerun this project or a process to run the tool first.  
- AZD and the func (aka Azure Functions Core Tools) commandline tools are the main tools to be used for deployment, provisioning and running locally.  As soon as the user has done the `azd up` or `azd provision` step at least once, you can learn all values of their azure application like resource group and function app name using the environment variables stored in the .azure folder.  Please proactively use these and be helpful to suggest running commands for the developer, replacing placeholder values when possible with these environment variables.
- This particular project is dotnet-isolated (.NET 10) Azure Function in C#
- We prefer using Azure Functions bindings if they can work versus the Azure SDKs, but Azure SDKs are ok if there is no substitute.  
