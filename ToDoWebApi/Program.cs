using FeatureHubSDK;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


IFeatureHubConfig config = new EdgeFeatureHubConfig(builder.Configuration["FeatureHub:Host"], builder.Configuration["FeatureHub:ApiKey"]);

config.Repository.ReadynessHandler += (sender, readiness) =>
{
    Console.WriteLine($"Readyness is $readiness");
};

FeatureLogging.DebugLogger += (sender, s) => Console.WriteLine("DEBUG: " + s + "\n");
FeatureLogging.TraceLogger += (sender, s) => Console.WriteLine("TRACE: " + s + "\n");
FeatureLogging.InfoLogger += (sender, s) => Console.WriteLine("INFO: " + s + "\n");
FeatureLogging.ErrorLogger += (sender, s) => Console.WriteLine("ERROR: " + s + "\n");
FeatureLogging.ExceptionLogger += (sender, s) => Console.WriteLine("ERROR: " + s.Message + "\n" + s.Exception );

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
config.Init();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

// add in the ToDo repository 
builder.Services.AddSingleton<ITodoServiceRepository, TodoServiceInMemoryRepository>();
;
// add in the featurehub config
builder.Services.AddSingleton(config);
// add in the controllers which use the featurehub and todo config
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.MapControllers();

app.Run();