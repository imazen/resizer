Tags: plugin
Edition: free
Bundle: free
Tagline: (default) - Sets Cache-control and Expires headers for optimum performance.
Aliases: /plugins/clientcache

# ClientCache plugin

This plugin has no effect when using the AsyncInterceptModule.

If an expiration duration is specified, ClientCache sends "Cache-control" and "Expires" HTTP headers to the client.  Installed, but not configured, by default.

IIS-level configuration (even in Web.config) can override the values set by ClientCache. Keep this in mind when troubleshooting issues.

## Configuration

1. Add `<clientcache minutes="1440" />` to the `<resizer />` section. This controls how many minutes in the future the Expires header will be set to.

## Default behavior

* `Cache-control: public` is sent for all anonymous requests.
* `Cache-control: private` is sent for all authenticated requests.
* `Expires` is sent only if 'minutes' is configured in web.config. For 24-hour expiration, use 1440 (suggested value).
* If DiskCache is in use, then a `Last-modified:` date for the cached file is sent. Otherwise, no Last-modified date can be sent.


