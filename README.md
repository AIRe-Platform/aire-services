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
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "PlatformConfiguration": "default",
    "TokenSigningKey": "<signing key shared between platform modules>",
    "TokenEncryptionKey": "<enryption key shared between platform modules>",
    "OpenApi__HostNames": "http://localhost:7071/api/"
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

The `test/CreateDefaultPlatform.http` file contains an example request for setting up default platform configuration. You can run the request directly from VS Code if you have the extension `humao.rest-client` installed.

## API Documentation

Visit path `/api/swagger/ui` to inspect. If running in localhost, there's an issue where the configuration file URL gets an invalid port.

Simply change in the correct port in the top bar to work around the issue.

Example: If the module is running on port `7071` change the URL to `http://localhost:7071/api/swagger.json`.

## Deployment

Publish the Fuctions app and then setup the following required environment values:

- `PlatformConfiguration` The name of the platform configuration to use. Default: `default`
- `TokenSigningKey` The token signing key shared between the platform instance modules.
- `TokenEncryptionKey` The token encryption key shared between the platform instance modules.

## Disclaimer

This README is a work-in-progress. The information above may be out-dated or incorrect.
