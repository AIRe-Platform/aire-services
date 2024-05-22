## TODO: Refactor this document to have an structure like this:
## Table of Contents
1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Usage](#usage)
4. [Configuration](#configuration)
5. [Contributing](#contributing)
6. [API Reference](#api-reference)
7. [Testing](#testing)
8. [Deployment](#deployment)
9. [Roadmap](#roadmap)
10. [FAQ](#faq)
11. [Troubleshooting](#troubleshooting)
12. [License](#license)
13. [Acknowledgments](#acknowledgments)
14. [Contact](#contact)

# AIRe Services

This module is the core of the AIRe platform. It manages all other modules in the AIRe platform instance.

A separate project called AIRe Hub will serve as a front-end for this module.

## Getting Started

You need to have [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed. Pull the repository and its submodules.

Open the solution in VS Code (recommended, works on Windows/Linux/macOS). You may also use Visual Studio on macOS and Windows.

On VS Code: Install the recommended extensions. Make sure the storage emulator (Azurite) is running all services.

Configure `local.settings.json` as instructed.

Hit F5 and you should be good to go.

## Configuration

You should create `local.settings.json` in the root of the repository when developing locally. It should look something like this:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "",
    "StorageConnectionString": "<Connection string for Table storage or storage emulator>",
    "PlatformConfiguration": "default",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AIRE_SERVICE_KEY": "<secret key between internal platform modules>",
    "TOKEN_SIGNING_KEY": "<signing key shared between platform modules>",
    "TOKEN_ENCRYPTION_KEY": "<enryption key shared between platform modules>"
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

When developing locally, you should generate random token/service keys and use them across the different modules. The keys are 32 characters in length. You could use random MD5 hash generator for this purpose.

The `test/CreateDefaultPlatform.http` file contains an example request for setting up the default platform configuration. You can run the request directly from VS Code if you have the extension `humao.rest-client` installed.

## API Documentation

Visit path `/api/swagger/ui` to inspect. The default host is set to `/api` path.

You can set a custom host with `OpenApi__HostNames` environment value.

## Deployment

Publish the Fuctions app and then setup the following required environment values:

- `PlatformConfiguration` The name of the platform configuration to use. Default: `default`
- `TOKEN_SIGNING_KEY` The token signing key shared between the platform instance modules.
- `TOKEN_ENCRYPTION_KEY` The token encryption key shared between the platform instance modules.
- `StorageConnectionString` Connection string for table storage. You probably want to use the same value as in `AzureWebJobsStorage`.
- `AIRE_SERVICE_KEY` The secret key shared between internal platform modules to authenticate service-to-service requests.

## Disclaimer

This README is a work-in-progress. The information above may be out-dated or incorrect.
