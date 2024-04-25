# Official FeatureHub .Net SDK

Welcome to the .Net SDK implementation for [FeatureHub.io](https://featurehub.io) - Open source Feature flags management, A/B testing and remote configuration platform.

## SDK features 
Details about what general features are available in FeatureHub SDKs are [available here](https://docs.featurehub.io/#_sdks).

## Changelog

- 2.4.0
  * fixed backoff delay in SSE
  * added support for polling API
  * added support for configuration using environment variables
- 2.3.0 - Support for preventing badly formatted API keys from being passed in. Support for API keys and cause 4xx errors
to stop polling. Support for overriding the number of backoff attempts made (`FEATUREHUB_BACKOFF_RETRY_LIMIT` - defaults to 100),
and delay retry timeout (was zero, now 10s, controlled by `FEATUREHUB_DELAY_RETRY_MS`). [DO NOT USE THIS VERSION]
- 2.2.0 - FeatureHub 1.5.9 support - supporting Fastly integration, server side polling period control, stale environments.
 We have upgraded to the 6.0.1 OpenAPI compiler, but gone no further because it generates code that does not work.
- 2.1.5 - FeatureHub 1.5.6 is not returning the name of the feature and this is causing the 2.1.4 to version to break.
- 2.1.4 - Bump dependencies version. Update source repository reference. 
- 2.1.3 - logging support (see below) and fixing of the backoff for the eventsource (it was randomly increasing the time, making features go out of date)
- 2.0.0 - client side evaluation support for feature strategies
- 1.1.0 - analytics support
- 1.0.0 - initial functionality with near-realtime event updates, full feature repository, server side rollout strategies.

## Connection choices

There are two connection choices in the SDK:

- realtime updates - if you have servers and applications that require updates in realtime, 
 we recommend you use the default connectivity this SDK provides, which is the event source.
- timeout based polling - if you have a requirement only to check features periodically, say once
every 3 minutes (or more), then you can use the Polling SDK. It operates by triggering on the same
`Init` method as the EventSource, but every time you evaluate a feature it will check if the timeout
has expired and if so, it will request an updated set of features in the background.

In all cases, you can synchronously wait for your features using either polling or the event source,
by just using an `await` when you use `Init` or `NewContext`. They will wait for a response to occur
whether it is success or failure. 

### Using the EventSource SDK

Find and copy your API Key from the FeatureHub Admin Console on the API Keys page -
you will use this in your code to configure feature updates for your environments.
It should look similar to this: ```71ed3c04-122b-4312-9ea8-06b2b8d6ceac/fsTmCrcZZoGyl56kPHxfKAkbHrJ7xZMKO3dlBiab5IqUXjgKvqpjxYdI8zdXiJqYCpv92Jrki0jY5taE```.
There are two options - a Server Evaluated API Key and a Client Evaluated API Key. More on this [here](https://docs.featurehub.io/#_client_and_server_api_keys)

In case of single user, desktop or embedded applications, use a server evaluated API key. If you are writing a batch, server or otherwise multi-user application, use a client-evaluated API key. This SDK does not support Xamarin.

There is a sample application included in the `ConsoleAppExample` folder
You could implement it in the following way:

```c#
// start by creating a IFeatureHubConfig object and telling it where your host server is and your
// client-evaluated API Key 
var config = new FeatureHubConfig("http://localhost:8903",
  "default/82afd7ae-e7de-4567-817b-dd684315adf7/SJXBRyGCe1dZ*PNYGy7iOFeKE");
  
config.Init(); // tell it to asynchronously connect and start listening
```

You can optionally set an analytics provider on the config (see below).

```c#
// this will set up a ClientContext - which is a bucket of information about this user
// and then attempt to connect to the repository and retrieve your data. It will return once it
// has received your data.  
var context = await config.NewContext().UserKey("ideally-unique-id")
        .Country(StrategyAttributeCountryName.Australia)
        .Device(StrategyAttributeDeviceName.Desktop)
        .Build();


// listen for changes to the feature FLUTTER_COLOUR and let me know what they are
context["FLUTTER_COLOUR"].FeatureUpdateHandler += (object sender, IFeatureStateHolder holder) =>
{
  Console.WriteLine($"Received type {holder.Key}: {context[holder.Key].StringValue}");        
};
```

There are many more convenience methods on the `IClientContext`, including:

    - IsEnabled - is this feature enabled?
    - IsSet - does this feature have a value?
    - LogAnalyticEvent - logs an analytics event if you have set up an analytics provider.

### Using Polling

You can use polling if you set the following environment variable: `FEATUREHUB_POLL_TIMEOUT` or
by calling `UsePolling(<timeout-in-seconds>)` on the `config`. 

### Configuring using Environment Variables

You can have the FeatureHub client automatically pick up the server configuration from two 
environment variables - `FEATUREHUB_EDGE_URL` provides the URL and `FEATUREHUB_API_KEY` provides
the key that you are using.

Then you can just use `new EdgeFeatureHubConfig()`.

### ASP.NET 

Wiring them into a ASP.NET application should also be fairly simple and it surfaces as an injectable service. Some example
code from our C# TodoServer in the `ToDoAspCoreExample` folder.

```c#
  private void AddFeatureHubConfiguration(IServiceCollection services)
  {
      IFeatureHubConfig config = new EdgeFeatureHubConfig(Configuration["FeatureHub:Host"], Configuration["FeatureHub:ApiKey"]);

      services.Add(ServiceDescriptor.Singleton(typeof(IFeatureHubConfig), config));

      config.Init();
  }
```

It is then available to be injected into your Controllers or Filters. 

### Rollout Strategies
Starting from version 1.1.0 FeatureHub supports _server side_ evaluation of complex rollout strategies
that are applied to individual feature values in a specific environment. This includes support of preset rules, e.g. per **_user key_**, **_country_**, **_device type_**, **_platform type_** as well as **_percentage splits_** rules and custom rules that you can create according to your application needs.

For more details on rollout strategies, targeting rules and feature experiments see the [core documentation](https://docs.featurehub.io/#_rollout_strategies_and_targeting_rules).

We are actively working on supporting client side evaluation of
strategies in the future releases as this scales better when you have 10000+ consumers.

#### Coding for Rollout strategies 
There are several preset strategies rules we track specifically: `user key`, `country`, `device` and `platform`. However, if those do not satisfy your requirements you also have an ability to attach a custom rule. Custom rules can be created as following types: `string`, `number`, `boolean`, `date`, `date-time`, `semantic-version`, `ip-address`

FeatureHub SDK will match your users according to those rules, so you need to provide attributes to match on in the SDK:

**Sending preset attributes:**

Provide the following attribute to support `userKey` rule:

```c#
    await context.UserKey("ideally-unique-id").Build(); 
```

to support `country` rule:
```c#
    await context.Country(StrategyAttributeCountryName.Australia).Build(); 
```

to support `device` rule:
```c#
    await context.Device(StrategyAttributeDeviceName.Desktop).Build(); 
```

to support `platform` rule:
```c#
    await context.Platform(StrategyAttributePlatformName.Android).Build(); 
```

to support `semantic-version` rule:
```c#
    await context.Version("1.2.0").Build(); 
```
or if you are using multiple rules, you can combine attributes as follows:

```c#
    await context.UserKey("ideally-unique-id")
      .Country(StrategyAttributeCountryName.NewZealand)
      .Device(StrategyAttributeDeviceName.Browser)
      .Platform(StrategyAttributePlatformName.Android)
      .Version("1.2.0")
      .Build(); 
```

For *Server Evaluated keys*, the  `Build()` method will trigger the regeneration of a 
special header (`x-featurehub`). This in turn will automatically retrigger a refresh of your events if 
you have already connected.

For *Client Evaluated API keys*, the `Build()` method does nothing, as all
the necessary decision making information is already available.

**Sending custom attributes:**

To add a custom key/value pair, use `Attr(key, value)`

```C#
    await context.Attr("first-language", "russian").Build();
```

Or with array of values (only applicable to custom rules):

```C#
   await context.Attrs("languages", new List<String> {"Russian", "English", "German"}).Build();
```

You can also use `featureHubRepository.ClientContext.Clear()` to empty your context.

In all cases, you need to call `Build()` to re-trigger passing of the new attributes to the server for recalculation.

### Analytics Support for C#

This allows you to connect your application and see your features performing in Google Analytics. The
adapter is generic but we provide specific support here for Google's Analytics platform at the moment.

When you log an event on the repository,
it will capture the value of all of the feature flags and featutre values (in case they change),
and log that event against your Google Analytics, once for each feature. This allows you to
slice and dice your events by state each of the features were in. We send them as a batch, so it
is only one request.

There is a plan to support other Analytics tools in the future. The only one we
currently support is Google Analytics, so you need:

- a Google analytics key - usually in the form `UA-123456`. You must provide this up front.
- a CID - a customer id this is associate with this. You can provide this up front or you can
provide it with each call, or you can set it later. 

1) You can set it in the constructor:

```c#
fhConfig.AddAnalyticCollector(new GoogleAnalyticsCollector("UA-example", "1234-5678-abcd-abcd",
new GoogleAnalyticsHttpClient()));
```

2) If you hold onto the Collector, you can set the CID on the collector later.

```c#
_collector.Cid = "some-value"; // you can set it here
```

3) When you log an event, you can pass it in the map:

```c#
var _data = new Dictionary<string, string>();
_data[GoogleConstants.Cid] = "some-cid";

context.LogAnalyticsEvent("event-name", _data);
```

Read more on how to interpret events in Google Analytics [here](https://docs.featurehub.io/analytics.html)

### Logging

This library doesn't "use" any of the various .NET logging systems, it simply exposes a static logger class, and
if you add events to this, you can see what is going on. This can be especially useful diagnosing connection issues
if you are having them. 

```c#
public static class FeatureLogging
{
  // Attach event handler to receive Trace level logs
  public static EventHandler<String> TraceLogger;
  // Attach event handler to receive Debug level logs
  public static EventHandler<String> DebugLogger;
  // Attach event handler to receive Info level logs
  public static EventHandler<String> InfoLogger;
  // Attach event handler to receive Error level logs
  public static EventHandler<String> ErrorLogger;
}
```

So a full diagnostic, as we have in our ASP.NET example looks like this:

```c#
  FeatureLogging.DebugLogger += (sender, s) => Console.WriteLine("DEBUG: " + s + "\n"); 
  FeatureLogging.TraceLogger += (sender, s) => Console.WriteLine("TRACE: " + s + "\n"); 
  FeatureLogging.InfoLogger += (sender, s) => Console.WriteLine("INFO: " + s + "\n"); 
  FeatureLogging.ErrorLogger += (sender, s) => Console.WriteLine("ERROR: " + s + "\n"); 
```

You can connect it to the logger of your choice.
