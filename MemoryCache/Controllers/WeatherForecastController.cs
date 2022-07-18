using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace MemoryCache.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };
                
        private readonly IConfiguration _configuration;        
        public WeatherForecastController(IConfiguration configuration)
        {            
            _configuration = configuration;            
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            IEnumerable<WeatherForecast>? resultado;

            var lazyConn = new Lazy<ConnectionMultiplexer>(() =>
            {
                var cacheConnString = _configuration.GetConnectionString("CacheConnection");
                return ConnectionMultiplexer.Connect(cacheConnString);
            });

            IDatabase cache = lazyConn.Value.GetDatabase();
            var resultadoCache = cache.StringGet("Temperature");            
            
            if (!resultadoCache.HasValue)
            {
                resultado = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                })
                .ToArray();
                cache.StringSet("Temperature", JsonConvert.SerializeObject(resultado), expiry: TimeSpan.FromSeconds(30));
            }
            else            
                resultado = JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(resultadoCache);

            return resultado;
        }
    }
}