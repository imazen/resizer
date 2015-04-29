Tags: plugin
Edition: free
Bundle: free
Tagline: (default) - Sets Cache-control, Expires, and Last-modified headers for optimum performance.
Aliases: /plugins/clientcache

# ClientCache plugin

If an expiration duration is specified, ClientCache sends "Cache-control" and "Expires" HTTP headers to the client.  Installed by default.

IIS-level configuration (even in Web.config) can override the values set by ClientCache. Keep this in mind when troubleshooting issues.

## Configuration

1. Add `<clientcache minutes="1440" />` to the `<resizer />` section. This controls how many minutes in the future the Expires header will be set to.

## Default behavior

`Cache-control: public` is sent for all anonymous requests.
`Cache-control: private` is sent for all authenticated requests.
`Last-modified:` is sent based on the modified date of the source file.
`Expires` is sent only if 'minutes' is configured in web.config. For 24-hour expiration, use 1440 (suggested value).


