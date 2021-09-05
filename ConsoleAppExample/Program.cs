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

    async static Task MainApp()
    {
      Console.WriteLine("Hello World!");

      var config = new EdgeFeatureHubConfig("http://localhost:8064",
        "default/b63faed0-1a66-48a6-8679-e362d1d33d7d/QgoWPhyDisWwHY5heuVzXbB41xqXwB*fL7QfLvLTb6OA00Cx8t1");
      config.Init();

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

        await context.UserKey("AmyWiles").Country(StrategyAttributeCountryName.Unitedkingdom).Attr("city", "london").Build();
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
