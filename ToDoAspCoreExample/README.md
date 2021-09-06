# ToDoAspCoreExample - ASP.NET Core 3.1 Server

## Description
Backend Todo server app to demonstrate the usage of FeatureHub .Net SDK

Demonstrates the Feature Flag ("BOOLEAN" type)

Feature key: "FEATURE_TITLE_TO_UPPERCASE"

If "FEATURE_TITLE_TO_UPPERCASE" is enabled - it will convert todo 'title' property to uppercase for every todo in the response for add/list/delete/resolve operations.

If this feature is disabled it will have todo 'title' in whatever format it was sent when a todo was created.

In addition, it demonstrates integration with Google Analytics.

## Prerequisites

* You need to setup a feature of type "Feature flag - boolean" in the FeatureHub Admin Console.
  Set feature key to "FEATURE_TITLE_TO_UPPERCASE".

* You are required to have a Service Account created in the FeatureHub Admin Console with the "read" permissions for your desired environment.
  Once this is set, copy the API Key (Client eval key) from the API Keys page for your desired environment and set it in the `appsettings.json` file:

```json
  "FeatureHub": {
    "Host": "http://localhost:8903",
    "ApiKey": "default/82afd7ae-e7de-4567-817b-dd684315adf7/SHxmTA83AJupii4TsIciWvhaQYBIq2*JxIKxiUoswZPmLQAIIWN"
  }
```

* If you like to see events fire in your Google Analytics, you will require to have valid GA Tracking ID, e.g. 'UA-XXXXXXXXX-X'.

You also need to specify a CID - a customer id this is associate with GA. In this example it is set to a random number.

Read more about CID https://stackoverflow.com/questions/14227331/what-is-the-client-id-when-sending-tracking-data-to-google-analytics-via-the-mea[here]

GA events:

`name: "todo-add", value: "10"`

`name: "todo-delete", value: "5"`

Once you launch the server, any call to "add" or "delete" to-do will generate a GA event accordingly.

More on GA integration can be found here https://docs.featurehub.io/analytics.html

## Running the example


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
cd src/ToDoAspCoreExample
docker build -t todoaspcore .
docker run -p 8099:8099 todoaspcore
```


You should see `"Features are available..."` message in the console. If no message shows up, it is likely the API Url is incorrect.

once the app is running, you should be able to do:


```
curl -X POST \
http://0.0.0.0:8099/todo/add \
-H 'Content-Type: application/json' \
-d '{"title": "Hello World", "id": "456"}'
```

and to get the list of to-dos:

```
curl -X GET \
http://0.0.0.0:8099/todo/list \
-H 'Postman-Token: 6bfe318a-5481-4e8e-a3e4-ab881202ba31' \
-H 'cache-control: no-cache'
```

Watch how "title" value in the response changes from lower case to upper case when you turn feature on/off from the FeatureHub Admin Console

Key areas of interest in this app, as much is generated from the OpenAPI C# tool are:

- src/Startup.cs - where the FeatureHubConfg is wired up.
- src/Controllers/HealthController.cs - where the health check is located
- src/Controllers/TodoServiceApi.cs - where the body of the TODO api is and where the features are being used

