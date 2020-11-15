using AspNetCorePureDiApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCorePureDiApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HelloController : ControllerBase
    {
        private readonly IDependency _scopedDependency;
        private readonly IDependency _singletonDependency;

        public HelloController(
            IDependency singletonDependency,
            IDependency scopedDependency)
        {
            _singletonDependency = singletonDependency;
            _scopedDependency = scopedDependency;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Ok($"Hello from controller with {_singletonDependency} and {_scopedDependency}!");
        }
    }
}