namespace CinemaTicketBooking.Api.Endpoints.Common;

public static class EndpointExtensions
{
    /// <summary>
    ///     Add automatically all endpoints which represent IEndpoints interface
    /// </summary>
    /// <typeparam name="TMarker">marker located in the same project as endpoints</typeparam>
    /// <param name="app">IApplicationBuilder</param>
    public static void UseEndpoints<TMarker>(this IApplicationBuilder app)
    {
        UseEndpoints(app, typeof(TMarker));
    }

    /// <summary>
    ///     Add automatically all endpoints which represent IEndpoints interface
    /// </summary>
    /// <param name="app">IApplicationBuilder</param>
    /// <param name="typeMarker">
    ///     marker located in the same project as endpoints<</param>
    public static void UseEndpoints(this IApplicationBuilder app, Type typeMarker)
    {
        var endpointTypes = typeMarker.Assembly.DefinedTypes
            .Where(x => !x.IsAbstract && !x.IsInterface && typeof(IEndpoints).IsAssignableFrom(x));

        foreach (var endpointType in endpointTypes)
            endpointType.GetMethod(nameof(IEndpoints.DefineEndpoints))!
                .Invoke(null, new object[] { app });
    }
}