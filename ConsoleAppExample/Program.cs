using System;
using System.Threading.Tasks;
using FeatureHubSDK;
using IO.FeatureHub.SSE.Model;

namespace ConsoleAppExample
{
  class Program
  {
    static void FeatureChanged(object sender, IFeature holder)
    {
      Console.WriteLine($"Received type {holder.Key}: {holder.StringValue}");
    }

    static void Main(string[] args)
    {
      MainApp().Wait();
    }

    static async Task MainApp()
    {
      Console.WriteLine("Hello World!");

      FeatureLogging.TraceLogger += (sender, s) => Console.WriteLine(s); 
      FeatureLogging.ErrorLogger += (sender, s) => Console.WriteLine(s); 
      FeatureLogging.DebugLogger += (sender, s) => Console.WriteLine(s); 
      FeatureLogging.InfoLogger += (sender, s) => Console.WriteLine(s);

      // var serverEvalKey = "d8cdd2f2-6003-4136-ad99-ee05730dfd97/YPSbDzRdrepVoTHUSm0IU8Da0hjJmZYxBG03jCEK";
      // var clientEvalKey = "d8cdd2f2-6003-4136-ad99-ee05730dfd97/gC3QHHb6mFQfNSerIItsCUUGZJL8aK*YyIiRr6cn5i5vN7eGtRA"; 
      // var config = new EdgeFeatureHubConfig("http://localhost:8903",
      //   serverEvalKey).UsePolling(10); // every 10 seconds
      var config = new EdgeFeatureHubConfig(); // use environment variables
      Console.WriteLine("Waiting for state to arrive");
      await config.Init();
      Console.WriteLine($"State should have arrived {config.Repository.Readyness}");

      Console.WriteLine($"Server evaluated {config.ServerEvaluation}");


      var fh = config.Repository;
      if (config.ServerEvaluation)
      {
        fh.GetFeature("FLUTTER_COLOUR").FeatureUpdateHandler += (object sender, IFeature holder) =>
        {
          Console.WriteLine($"Received type {holder.Key}: {holder.StringValue}");
        };
      }
      else
      {
        Console.WriteLine("Using client side validation");
      }

      fh.ReadynessHandler += (sender, readyness) =>
      {
        Console.WriteLine($"Readyness is {readyness}");
      };

      fh.NewFeatureHandler += (sender, repository) =>
      {
        Console.WriteLine($"New features");
      };

      // fh.AddAnalyticCollector(new GoogleAnalyticsCollector("UA-example", "1234-5678-abcd-abcd",
      //   new GoogleAnalyticsHttpClient()));

      // do
      // {
      //   fh.LogAnalyticEvent("c-sharp-console");
      //   Console.Write("Press a Key");
      // } while (Console.ReadLine() != "x");


      Console.WriteLine("Context initialized, waiting for readyness - Press a key when readyness appears");
      Console.ReadKey();

      // this will set up a ClientContext - which is a bucket of information about this user
// and then attempt to connect to the repository and retrieve your data. It will return once it
// has received your data.
      var context = await config.NewContext().UserKey("ideally-unique-id")
        .Country(StrategyAttributeCountryName.Australia)
        .Device(StrategyAttributeDeviceName.Desktop)
        .Build();


      if (fh.Readyness == Readyness.Ready)
      {
        Console.Write("Press a key (changed context)");

        Func<bool?> val = () => context["FEATURE_TITLE_TO_UPPERCASE"].BooleanValue;

        await context.UserKey("DJElif").Country(StrategyAttributeCountryName.Turkey).Attr("city", "istanbul").Build();

        Console.WriteLine($"Istanbul 1 is {val()}");
        Console.ReadKey();
        Console.WriteLine($"Istanbul 2 is {val()}");

        Console.Write("Press a key (change context2)");
        Console.ReadKey();

        await context.UserKey("AmyWiles").Country(StrategyAttributeCountryName.UnitedKingdom).Attr("city", "london").Build();
        Console.WriteLine($"london 1 is {val()}");
        // Console.ReadKey();
        Console.WriteLine($"london 1 is {val()}");
        Console.WriteLine("Ready to close");
        // Console.ReadKey();
      }
      else
      {
        Console.WriteLine("Not ready yet");
      }
      
      context.Close();
    }
  }
}
