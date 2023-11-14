# AIRe Services

This module is the core of the AIRe platform. It manages all other modules in the AIRe platform instance.

A separate project called AIRe Hub will serve as a front-end for this module.

## Getting Started

Open the solution in VS Code (on Windows/Linux/macOS). Install the recommended extensions. Hit F5 and you should be good to go.

You may also use Visual Studio on macOS and Windows.

Remember to configure!

## Configuration

You should create `local.settings.json` in the root of the repository when developing locally. It should look something like this:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "",
    "StorageConnectionString": "<Connection string for Table storage or storage emulator>",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "ID_MODULE_ENDPOINT": "http://localhost:7072/api",
    "MEMORY_MODULE_ENDPOINT": "http://localhost:7073/api",
    "AI_MODULE_ENDPOINT": "http://localhost:7074/api",
    "PLATFORM_CONFIGURATION": "default"
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

The `test/CreateDefaultPlatform.http` file contains an example request for setting up default platform configuration. You can run the request directly from VS Code if you have the extension `humao.rest-client` installed.

## Deployment

Publish the Functions app and then setup the following required environment values:

- Default platform configuration name: `PLATFORM_CONFIGURATION`
- Storage connection string: `StorageConnectionString` (note: probably the same as your `AzureWebJobsStorage`)
- Endpoints: `ID_MODULE_ENDPOINT`, `MEMORY_MODULE_ENDPOINT`, `AI_MODULE_ENDPOINT`
