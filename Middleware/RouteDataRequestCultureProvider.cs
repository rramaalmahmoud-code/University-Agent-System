//using Microsoft.AspNetCore.Localization;
//using Microsoft.AspNetCore.Routing;
//using System.Globalization;
//using System.Threading.Tasks;

//public class RouteDataRequestCultureProvider : RequestCultureProvider
//{
//    public string RouteDataStringKey { get; set; } = "culture"; // Default key for culture in the route

//    public override Task<ProviderCultureResult> DetermineProviderCultureResult(Microsoft.AspNetCore.Http.HttpContext httpContext)
//    {
//        // Get the route values from the current context
//        var routeValues = httpContext.GetRouteData()?.Values;

//        // Check if the route contains the culture value
//        if (routeValues != null && routeValues.TryGetValue(RouteDataStringKey, out var cultureObj))
//        {
//            // Extract culture from the route data
//            var culture = cultureObj?.ToString();
//            if (!string.IsNullOrEmpty(culture))
//            {
//                return Task.FromResult(new ProviderCultureResult(culture, culture)); // Return the culture
//            }
//        }

//        // If no culture is found, return null (so fallback culture is used)
//        return Task.FromResult<ProviderCultureResult>(null);
//    }
//}
