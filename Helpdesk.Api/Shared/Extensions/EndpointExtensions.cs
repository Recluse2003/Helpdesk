using Helpdesk.Api.Shared.Interfaces;

namespace Helpdesk.Api.Shared.Extensions
{
    /// <summary>
    /// Provides extension methods for registering application feature endpoints.
    /// </summary>
    public static class EndpointExtensions
    {
        /// <summary>
        /// Finds and registers all concrete endpoint implementations in the API assembly. 
        /// </summary>
        /// <param name="app">The endpoint route builder used to register application endpoints.</param>
        /// <returns>The same <see cref="IEndpointRouteBuilder"/> instance for method chaining.</returns>
        public static IEndpointRouteBuilder MapFeatures(this IEndpointRouteBuilder app)
        {
            // Scans through the main API assembly for any concrete class that implements IEndpoint
            var endpointTypes = typeof(Program).Assembly.GetTypes()
                .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            // Loop through them, instantiate them, and call their MapEndpoint method
            foreach (var type in endpointTypes)
            {
                var endpoint = (IEndpoint)Activator.CreateInstance(type)!;
                endpoint.MapEndpoint(app);
            }

            return app;
        }
    }
}
