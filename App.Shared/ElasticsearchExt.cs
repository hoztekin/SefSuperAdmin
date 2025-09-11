using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace App.Shared
{
    public static class ElasticsearchExt
    {

        public static void AddElastic(this IServiceCollection services, IConfiguration configuration)
        {

            var pool = new SingleNodeConnectionPool(new Uri(configuration.GetSection("Elastic")["Url"]!));


            var settings = new ConnectionSettings(pool);


            var client = new ElasticClient(settings);

            services.AddSingleton(client);

        }
    }
}
