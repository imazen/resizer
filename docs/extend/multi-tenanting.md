Aliases: /docs/howto/multi-tenanting /docs/extend/multi-tenanting

# Support multi-tenanting


The ImageResizer architecture is designed to make multi-tenanting support easy to implement; simply implement `IPlugin` and `ICurrentConfigProvider`. A `Config` instance can be created from an XML node or string.




      /// <summary>
      /// Allows multi-tenancy support. The 'root' config only needs one plugin, which implements this interface.
      /// </summary>
      public interface ICurrentConfigProvider {
          /// <summary>
          /// Returns a Config instance appropriate for the current request. If null is returned, the default/root instance will be used.
          /// Implementations MUST return the same instance of Config for two identical requests. Multiple Config instances per tenant/area will cause problems.
          /// MUST be thread-safe, concurrent calls WILL occur, and WILL occur during initial call. 
          /// </summary>
          /// <returns></returns>
          Config GetCurrentConfig();
      }