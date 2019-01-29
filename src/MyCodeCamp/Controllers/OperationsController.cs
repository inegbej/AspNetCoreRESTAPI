/**/

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace MyCodeCamp.Controllers
{
    [Route("api/[controller]")]
    public class OperationsController : Controller
    {
        private IConfigurationRoot _config;
        private ILogger<OperationsController> _logger;

        public OperationsController(ILogger<OperationsController> logger, IConfigurationRoot config)
        {
            _logger = logger;
            _config = config;
        }

        [HttpOptions("reloadConfig")]
        public IActionResult ReloadConfiguration()
        {
            try
            {
                _config.Reload();            // reload configuration bundle

                return Ok("Configuration Reloaded");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown while reloading configuration: {ex}");
            }

            return BadRequest("Could not reload configuration");
        }
    }
}
