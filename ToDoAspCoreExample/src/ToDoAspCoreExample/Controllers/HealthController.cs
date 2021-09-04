using FeatureHubSDK;
using Microsoft.AspNetCore.Mvc;

namespace ToDoAspCoreExample.Controllers
{
    public class HealthController : Controller
    {
        private readonly IFeatureHubConfig fhConfig;

        public HealthController(IFeatureHubConfig fhConfig)
        {
            this.fhConfig = fhConfig;
        }

        // GET
        [Route("/health/liveness")]
        public IActionResult Liveness()
        {
            return fhConfig.Readyness == Readyness.Ready ? Ok() : StatusCode(503);
        }
    }
}
