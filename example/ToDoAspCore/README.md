# ToDoAspCore - ASP.NET Core 3.1 Server

This is an implementation of the same todo server as per Typescript and Java, and uses the ASP.NET Core framework.

It expects the use of a client evaluated API key, and should be stored in the `appsettings.json` file:

```json
  "FeatureHub": {
    "Host": "http://localhost:8903",
    "ApiKey": "default/82afd7ae-e7de-4567-817b-dd684315adf7/SHxmTA83AJupii4TsIciWvhaQYBIq2*JxIKxiUoswZPmLQAIIWN"
  }
```

Once you have started it using one of the following mechanisms you can use the Todo-React app to talk to it.

Key areas of interest in this app, as much is generated from the OpenAPI C# tool are:

- src/Startup.cs - where the FeatureHubConfg is wired up.
- src/Controllers/HealthController.cs - where the health check is located
- src/Controllers/TodoServiceApi.cs - where the body of the TODO api is and where the features are being used

## Run

Linux/OS X:

```
sh build.sh
```

Windows:

```
build.bat
```
## Run in Docker

```
cd src/ToDoAspCore
docker build -t todoaspcore .
docker run -p 8099:8099 todoaspcore
```
