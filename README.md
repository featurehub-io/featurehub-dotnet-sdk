# Official FeatureHub .Net SDK

Welcome to the .Net SDK implementation for [FeatureHub.io](https://featurehub.io) - Open source Feature flags management, A/B testing and remote configuration platform.

## SDK features 
Details about what general features are available in FeatureHub SDKs are [available here](https://docs.featurehub.io/#_sdks).

## Changelog
- 3.0.1 
  * Documentation updates 
- 3.0.0
  * Updated for .NET 8+
  * Updated library support
  * Updated examples to match latest style
- 2.5.1
  * Add support for a concurrent dictionary for features. This prevents features which are requested before the Repository is ready from
    clashing in a concurrent situation.
- 2.5.0
  * Update dependency - EventSource library to version 4.2
  * Remove Google Listener as it is for the old standard which expired mid-2023. There is currently no replacement for it until the new usage API comes in.
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

### Reading feature values

Features are read through `IFeature`, returned by indexing a context or the repository directly:

```c#
IFeature flag = context["MY_FLAG"];
```

**Typed accessors with defaults** — safe to call even before the repository is ready:

```c#
bool   on      = context["DARK_MODE"].Boolean(defaultValue: false);
string colour  = context["BRAND_COLOUR"].String(defaultValue: "blue");
double timeout = context["TIMEOUT_MS"].Number(defaultValue: 5000);
string config  = context["APP_CONFIG"].Json(defaultValue: "{}");
```

**Nullable property accessors** — return `null` when the feature doesn't exist or has no value:

```c#
bool?   boolVal   = context["DARK_MODE"].BooleanValue;
string? strVal    = context["BRAND_COLOUR"].StringValue;
double? numVal    = context["TIMEOUT_MS"].NumberValue;
string? jsonVal   = context["APP_CONFIG"].JsonValue;
```

**State helpers:**

```c#
context["MY_FLAG"].IsEnabled  // true if boolean feature is true
context["MY_FLAG"].IsSet      // true if feature has a non-null value
context["MY_FLAG"].IsLocked   // true if the feature is locked server-side
context["MY_FLAG"].Exists     // true if the feature is known to the repository
context["MY_FLAG"].Type       // FeatureValueType.BOOLEAN / STRING / NUMBER / JSON, or null
context["MY_FLAG"].Version    // long? version number
```

The context shortcuts `context.IsEnabled(name)` and `context.IsSet(name)` are equivalent to the
property forms above.

### Listening for feature changes

Every `IFeature` exposes a `FeatureUpdateHandler` event that fires whenever that feature's value
changes. You can subscribe before the repository is ready — the handler will fire once the first
value arrives and again on every subsequent change:

```c#
context["DARK_MODE"].FeatureUpdateHandler += (sender, feature) =>
{
    Console.WriteLine($"DARK_MODE changed to {feature.BooleanValue}");
};
```

The event passes the updated `IFeature` as its argument. You can also listen for *any* feature
change on the repository:

```c#
config.Repository.NewFeatureHandler += (sender, repo) =>
{
    Console.WriteLine("One or more features changed");
};
```

### Using Polling

You can use polling if you set the following environment variable: `FEATUREHUB_POLL_TIMEOUT` or
by calling `UsePolling(<timeout-in-seconds>)` on the `config`:

```c#
config.ActiveRest(120); // check for updates every 120 seconds
```

or

```c#
config.PassiveRest(120); // check for updates every at most every 120 seconds depending on feature evaluation
```

There are two polling modes:

- **Active polling** (default when `FEATUREHUB_POLL_TIMEOUT` is set) — fetches updated features
  on a fixed timer interval.
- **Passive polling** — only contacts the server when a feature is actually evaluated. Enable it by
  setting the `FEATUREHUB_POLLING_PASSIVE` environment variable (any value), or by using
  `EdgeType.PassiveRest` directly. In this mode, the SDK uses the usage event stream to detect that
  a feature has been read and triggers a background poll if the cache is stale.

```bash
# passive polling: fetch only when a feature is evaluated
FEATUREHUB_POLL_TIMEOUT=120 FEATUREHUB_POLLING_PASSIVE=true ./myapp
```

**Retry behaviour** — the following environment variables control how the SDK handles connection
failures for both SSE and polling:

| Variable | Default | Purpose |
|---|---|---|
| `FEATUREHUB_BACKOFF_RETRY_LIMIT` | 100 | Maximum number of reconnect attempts before giving up |
| `FEATUREHUB_DELAY_RETRY_MS` | 10000 | Delay in milliseconds between retry attempts |

### Configuring using Environment Variables

You can have the FeatureHub client automatically pick up the server configuration from environment
variables:

| Variable | Purpose |
|---|---|
| `FEATUREHUB_EDGE_URL` | URL of the FeatureHub Edge server |
| `FEATUREHUB_API_KEY` | SDK key (or comma-separated list of keys — see below) |
| `FEATUREHUB_POLL_TIMEOUT` | Enables polling mode; value is the interval in seconds |
| `FEATUREHUB_POLLING_PASSIVE` | When set alongside `FEATUREHUB_POLL_TIMEOUT`, enables passive polling |
| `FEATUREHUB_BACKOFF_RETRY_LIMIT` | Max reconnect attempts (default 100) |
| `FEATUREHUB_DELAY_RETRY_MS` | Delay between retries in milliseconds (default 10000) |

```bash
FEATUREHUB_EDGE_URL=https://edge.example.com FEATUREHUB_API_KEY=<sdk-key> ./myapp
```

Then you can just use `new EdgeFeatureHubConfig()`.

### Multiple SDK keys

`EdgeFeatureHubConfig` accepts a single SDK key, but the underlying `SdkKeys` property is a
`List<string>`, allowing multiple keys to be registered on the same config. This is useful when
you need to fan out to multiple environments or tenants from a single process. Add additional keys
after construction:

```c#
var config = new EdgeFeatureHubConfig("https://edge.example.com", primaryKey);
config.SdkKeys.Add(secondaryKey);
```

Please note this only works for REST, not Streaming.

### Readiness and health checks

The SDK exposes a `Readiness` property that reflects the current connection state:

| Value | Meaning |
|---|---|
| `Readiness.NotReady` | Not yet connected, or connection was lost |
| `Readiness.Ready` | Features have been received and are available |
| `Readiness.Failed` | A permanent failure occurred (e.g. invalid API key) |

```c#
// suitable for a liveness or readiness probe
if (config.Readiness == Readiness.Ready)
{
    // safe to serve traffic
}
```

You can also react to readiness changes with an event:

```c#
config.Repository.ReadinessHandler += (sender, readiness) =>
{
    Console.WriteLine($"Repository is now {readiness}");
};
```

When `await config.Init()` or `await context.Build()` completes, the repository will be either
`Ready` or `Failed` — it never returns while still `NotReady`.

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


### Usage tracking / analytics

Every time a feature value is read through a context, the SDK emits a usage event onto an internal
stream. You can tap this stream to send analytics data to any backend (e.g. Google Analytics,
Amplitude, a custom data warehouse).

#### Writing a plugin

Subclass `UsagePlugin` and implement `Send`:

```c#
public class MyAnalyticsPlugin : UsagePlugin
{
    public override void Send(IUsageEvent usageEvent)
    {
        if (usageEvent is IUsageEventWithFeature featureEvent)
        {
            // featureEvent.Feature.Key   — the feature key
            // featureEvent.Feature.Value — serialised value: "on"/"off", number string, raw string
            // featureEvent.Feature.Type  — FeatureValueType
            // featureEvent.Attributes    — context attributes at evaluation time
            // featureEvent.UserKey       — user/session key, if set on the context
            MyBackend.Track(featureEvent.Feature.Key, featureEvent.Feature.Value);
        }
    }
}
```

The `IUsageEvent.CopyBaseMap()` method returns a flat `IReadOnlyDictionary<string, object?>` that
merges all event fields — useful if your backend expects a property bag.

#### Wiring up the adapter

`UsageAdapter` subscribes to the repository stream and fans events out to all registered plugins,
catching and logging exceptions from individual plugins so one bad plugin cannot affect others:

```c#
var adapter = new UsageAdapter(config.Repository);
adapter.RegisterPlugin(new MyAnalyticsPlugin());

// when tearing down (e.g. app shutdown):
adapter.Close();
```

#### Customising value serialisation

By default, boolean features serialise as `"on"`/`"off"`, numbers and strings as their string
representation, and JSON features as `null`. Replace `DefaultUsageProvider.Convert` to change this
globally:

```c#
DefaultUsageProvider.Convert = (value, type) =>
    type == FeatureValueType.BOOLEAN
        ? (true.Equals(value) ? "true" : "false")
        : DefaultUsageProvider.DefaultConvert(value, type);
```

#### Customising event objects

To attach extra fields to every event, replace the provider on the repository:

```c#
config.Repository.RegisterUsageProvider(new MyUsageProvider());
```

`MyUsageProvider` implements `IUsageProvider` (or subclasses `BaseUsageProvider`) and returns
custom event objects that carry whatever additional data you need.

### Feature Value Interceptors

Feature value interceptors let you override feature values locally — useful for local development,
testing, or emergency kill-switches — without changing anything in the FeatureHub Admin Console.

Interceptors implement `IFeatureValueInterceptor` and are registered on the repository:

```c#
public interface IFeatureValueInterceptor
{
    // When true, this interceptor is called even for locked features.
    bool AllowLockOverride { get; }
    // Return (true, value) to override, or (false, null) to pass through.
    (bool, object?) GetValue(string key, FeatureState? featureState);
}
```

```c#
// register
config.AddFeatureValueInterceptor(myInterceptor);
```

#### LocalYamlValueInterceptor

`LocalYamlValueInterceptor` reads overrides from a YAML file with a single `flagValues` map.
Values are typed automatically from their YAML representation:

| YAML value | Inferred type | C# value |
|---|---|---|
| `true` / `false` (unquoted) | `BOOLEAN` | `bool` |
| `42` / `3.14` (unquoted) | `NUMBER` | `double` |
| `hello` / `"quoted string"` | `STRING` | `string` |
| nested map or sequence | `JSON` | JSON `string` |

Example YAML file (`local-overrides.yaml`):

```yaml
flagValues:
  dark-mode: true
  max-retries: 5
  welcome-message: "Hello, developer!"
  feature-config:
    timeout: 30
    enabled: true
```

Wire it up at startup:

```c#
var interceptor = new LocalYamlValueInterceptor("local-overrides.yaml");
config.Repository.AddFeatureValueInterceptor(interceptor);
```

The file is read once at construction time. If the file does not exist the interceptor silently
passes all lookups through. Locked features are **not** overridden (`AllowLockOverride` is `false`).

#### Writing a custom interceptor

```c#
public class MyInterceptor : IFeatureValueInterceptor
{
    public bool AllowLockOverride => false;

    public (bool, object?) GetValue(string key, FeatureState? featureState)
    {
        if (key == "maintenance-mode")
            return (true, true); // always on locally
        return (false, null);    // let FeatureHub decide everything else
    }
}
```

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
