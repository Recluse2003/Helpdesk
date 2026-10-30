using Helpdesk.Api.Shared.Interfaces;

namespace Helpdesk.Api.Shared.Extensions
{
    public static class EndpointExtensions
    {
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
