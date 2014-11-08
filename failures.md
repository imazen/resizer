# Things we've tried that didn't work
### SIMD vectorization and planar memory layouts. SIMD requires a planar memory layout, either in the image structure or the resize buffer (we've tried both). Planar memory layouts tend to cause CPU cache-misses. CPU cache misses are far more expensive than lack of SIMD instructions.

### Automatic module installation

Dynamically added modules come with precondition="managedHandler", which is a deal-breaker, since we deal with static files.

    public class InterceptModule : IHttpModule {
        public static void PreApplicationStart()
        {
            //Only register module if we're running on MSFT .NET. Mono support for RegisterModule isnt' here yet.
            if (System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath != null && Type.GetType("Mono.Runtime") == null){
                HttpApplication.RegisterModule(typeof(InterceptModule));
            }
        }


