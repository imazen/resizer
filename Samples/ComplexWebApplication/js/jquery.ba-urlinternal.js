/*!
 * urlInternal - v1.0 - 10/7/2009
 * http://benalman.com/projects/jquery-urlinternal-plugin/
 * 
 * Copyright (c) 2009 "Cowboy" Ben Alman
 * Dual licensed under the MIT and GPL licenses.
 * http://benalman.com/about/license/
 */

// Script: jQuery urlInternal: Easily test URL internal-, external or fragment-ness
// 
// *Version: 1.0, Last updated: 10/7/2009*
// 
// Project Home - http://benalman.com/projects/jquery-urlinternal-plugin/
// GitHub       - http://github.com/cowboy/jquery-urlinternal/
// Source       - http://github.com/cowboy/jquery-urlinternal/raw/master/jquery.ba-urlinternal.js
// (Minified)   - http://github.com/cowboy/jquery-urlinternal/raw/master/jquery.ba-urlinternal.min.js (1.7kb)
// 
// About: License
// 
// Copyright (c) 2009 "Cowboy" Ben Alman,
// Dual licensed under the MIT and GPL licenses.
// http://benalman.com/about/license/
// 
// About: Examples
// 
// This working example, complete with fully commented code, illustrates a few
// ways in which this plugin can be used.
// 
// http://benalman.com/code/projects/jquery-urlinternal/examples/urlinternal/
// 
// About: Support and Testing
// 
// Information about what version or versions of jQuery this plugin has been
// tested with, what browsers it has been tested in, and where the unit tests
// reside (so you can test it yourself).
// 
// jQuery Versions - 1.3.2
// Browsers Tested - Internet Explorer 6-8, Firefox 2-3.7, Safari 3-4, Chrome, Opera 9.6-10.
// Unit Tests      - http://benalman.com/code/projects/jquery-urlinternal/unit/
// 
// About: Release History
// 
// 1.0 - (10/7/2009) Initial release

(function($){
  '$:nomunge'; // Used by YUI compressor.
  
  // Some convenient shortcuts.
  var undefined,
    TRUE = !0,
    FALSE = !1,
    loc = window.location,
    aps = Array.prototype.slice,
    
    matches = loc.href.match( /^((https?:\/\/.*?\/)?[^#]*)#?.*$/ ),
    loc_fragbase = matches[1] + '#',
    loc_hostbase = matches[2],
    
    // Method references.
    jq_elemUrlAttr,
    jq_urlInternalHost,
    jq_urlInternalRegExp,
    jq_isUrlInternal,
    jq_isUrlExternal,
    jq_isUrlFragment,
    
    // Reused strings.
    str_elemUrlAttr = 'elemUrlAttr',
    str_href = 'href',
    str_src = 'src',
    str_urlInternal = 'urlInternal',
    str_urlExternal = 'urlExternal',
    str_urlFragment = 'urlFragment',
    
    url_regexp,
    
    // Used by jQuery.elemUrlAttr.
    elemUrlAttr_cache = {};
  
  // Why write the same function twice? Let's curry! Mmmm, curry..
  
  function curry( func ) {
    var args = aps.call( arguments, 1 );
    
    return function() {
      return func.apply( this, args.concat( aps.call( arguments ) ) );
    };
  };
  
  // Section: Methods
  // 
  // Method: jQuery.isUrlInternal
  // 
  // Test whether or not a URL is internal. Non-navigating URLs (ie. #anchor,
  // javascript:, mailto:, news:, tel:, im: or non-http/https protocol://
  // links) are not considered internal.
  // 
  // Usage:
  // 
  // > jQuery.isUrlInternal( url );
  // 
  // Arguments:
  // 
  //   url - (String) a URL to test the internal-ness of.
  // 
  // Returns:
  // 
  //  (Boolean) true if the URL is internal, false if external, or undefined if
  //  the URL is non-navigating.
  
  $.isUrlInternal = jq_isUrlInternal = function( url ) {
    
    // non-navigating: url is nonexistent or a fragment
    if ( !url || jq_isUrlFragment( url ) ) { return undefined; }
    
    // internal: url is absolute-but-internal (see $.urlInternalRegExp)
    if ( url_regexp.test(url) ) { return TRUE; }
    
    // external: url is absolute (begins with http:// or https:// or //)
    if ( /^(?:https?:)?\/\//i.test(url) ) { return FALSE; }
    
    // non-navigating: url begins with scheme:
    if ( /^[a-z\d.-]+:/i.test(url) ) { return undefined; }
    
    return TRUE;
  };
  
  // Method: jQuery.isUrlExternal
  // 
  // Test whether or not a URL is external. Non-navigating URLs (ie. #anchor,
  // mailto:, javascript:, or non-http/https protocol:// links) are not
  // considered external.
  // 
  // Usage:
  // 
  // > jQuery.isUrlExternal( url );
  // 
  // Arguments:
  // 
  //   url - (String) a URL to test the external-ness of.
  // 
  // Returns:
  // 
  //  (Boolean) true if the URL is external, false if internal, or undefined if
  //  the URL is non-navigating.
  
  $.isUrlExternal = jq_isUrlExternal = function( url ) {
    var result = jq_isUrlInternal( url );
    
    return typeof result === 'boolean'
      ? !result
      : result;
  };
  
  // Method: jQuery.isUrlFragment
  // 
  // Test whether or not a URL is a fragment in the context of the current page,
  // meaning the URL can either begin with # or be a partial URL or full URI,
  // but when it is navigated to, only the document.location.hash will change,
  // and the page will not reload.
  // 
  // Usage:
  // 
  // > jQuery.isUrlFragment( url );
  // 
  // Arguments:
  // 
  //   url - (String) a URL to test the fragment-ness of.
  // 
  // Returns:
  // 
  //  (Boolean) true if the URL is a fragment, false otherwise.
  
  $.isUrlFragment = jq_isUrlFragment = function( url ) {
    var matches = ( url || '' ).match( /^([^#]?)([^#]*#).*$/ );
    
    // url *might* be a fragment, since there were matches.
    return !!matches && (
      
      // url is just a fragment.
      matches[2] === '#'
      
      // url is absolute and contains a fragment, but is otherwise the same URI.
      || url.indexOf( loc_fragbase ) === 0
      
      // url is relative, begins with '/', contains a fragment, and is otherwise
      // the same URI.
      || ( matches[1] === '/' ? loc_hostbase + matches[2] === loc_fragbase
      
      // url is relative, but doesn't begin with '/', contains a fragment, and
      // is otherwise the same URI. This isn't even remotely efficient, but it's
      // significantly less code than parsing everything. Besides, it will only
      // even be tested on url values that contain '#', aren't absolute, and
      // don't begin with '/', which is not going to be many of them.
      : !/^https?:\/\//i.test( url ) && $('<a href="' + url + '"/>')[0].href.indexOf( loc_fragbase ) === 0 )
    );
  };
  
  // Method: jQuery.fn.urlInternal
  // 
  // Filter a jQuery collection of elements, returning only elements that have
  // an internal URL (as determined by <jQuery.isUrlInternal>). If URL cannot
  // be determined, remove the element from the collection.
  // 
  // Usage:
  // 
  // > jQuery('selector').urlInternal( [ attr ] );
  // 
  // Arguments:
  // 
  //  attr - (String) Optional name of an attribute that will contain a URL to
  //    test internal-ness against. See <jQuery.elemUrlAttr> for a list of
  //    default attributes.
  // 
  // Returns:
  // 
  //  (jQuery) A filtered jQuery collection of elements.
  
  // Method: jQuery.fn.urlExternal
  // 
  // Filter a jQuery collection of elements, returning only elements that have
  // an external URL (as determined by <jQuery.isUrlExternal>). If URL cannot
  // be determined, remove the element from the collection.
  // 
  // Usage:
  // 
  // > jQuery('selector').urlExternal( [ attr ] );
  // 
  // Arguments:
  // 
  //  attr - (String) Optional name of an attribute that will contain a URL to
  //    test external-ness against. See <jQuery.elemUrlAttr> for a list of
  //    default attributes.
  // 
  // Returns:
  // 
  //  (jQuery) A filtered jQuery collection of elements.
  
  // Method: jQuery.fn.urlFragment
  // 
  // Filter a jQuery collection of elements, returning only elements that have
  // an fragment URL (as determined by <jQuery.isUrlFragment>). If URL cannot
  // be determined, remove the element from the collection.
  // 
  // Note that in most browsers, selecting $("a[href^=#]") is reliable, but this
  // doesn't always work in IE6/7! In order to properly test whether a URL
  // attribute's value is a fragment in the context of the current page, you can
  // either make your selector a bit more complicated.. or use .urlFragment!
  // 
  // Usage:
  // 
  // > jQuery('selector').urlFragment( [ attr ] );
  // 
  // Arguments:
  // 
  //  attr - (String) Optional name of an attribute that will contain a URL to
  //    test external-ness against. See <jQuery.elemUrlAttr> for a list of
  //    default attributes.
  // 
  // Returns:
  // 
  //  (jQuery) A filtered jQuery collection of elements.
  
  function fn_filter( str, attr ) {
    return this.filter( ':' + str + (attr ? '(' + attr + ')' : '') );
  };
  
  $.fn[ str_urlInternal ] = curry( fn_filter, str_urlInternal );
  $.fn[ str_urlExternal ] = curry( fn_filter, str_urlExternal );
  $.fn[ str_urlFragment ] = curry( fn_filter, str_urlFragment );
  
  // Section: Selectors
  // 
  // Selector: :urlInternal
  // 
  // Filter a jQuery collection of elements, returning only elements that have
  // an internal URL (as determined by <jQuery.isUrlInternal>). If URL cannot
  // be determined, remove the element from the collection.
  // 
  // Usage:
  // 
  // > jQuery('selector').filter(':urlInternal');
  // > jQuery('selector').filter(':urlInternal(attr)');
  // 
  // Arguments:
  // 
  //  attr - (String) Optional name of an attribute that will contain a URL to
  //    test internal-ness against. See <jQuery.elemUrlAttr> for a list of
  //    default attributes.
  // 
  // Returns:
  // 
  //  (jQuery) A filtered jQuery collection of elements.
  
  // Selector: :urlExternal
  // 
  // Filter a jQuery collection of elements, returning only elements that have
  // an external URL (as determined by <jQuery.isUrlExternal>). If URL cannot
  // be determined, remove the element from the collection.
  // 
  // Usage:
  // 
  // > jQuery('selector').filter(':urlExternal');
  // > jQuery('selector').filter(':urlExternal(attr)');
  // 
  // Arguments:
  // 
  //  attr - (String) Optional name of an attribute that will contain a URL to
  //    test external-ness against. See <jQuery.elemUrlAttr> for a list of
  //    default attributes.
  // 
  // Returns:
  // 
  //  (jQuery) A filtered jQuery collection of elements.
  
  // Selector: :urlFragment
  // 
  // Filter a jQuery collection of elements, returning only elements that have
  // an fragment URL (as determined by <jQuery.isUrlFragment>). If URL cannot
  // be determined, remove the element from the collection.
  // 
  // Note that in most browsers, selecting $("a[href^=#]") is reliable, but this
  // doesn't always work in IE6/7! In order to properly test whether a URL
  // attribute's value is a fragment in the context of the current page, you can
  // either make your selector a bit more complicated.. or use :urlFragment!
  // 
  // Usage:
  // 
  // > jQuery('selector').filter(':urlFragment');
  // > jQuery('selector').filter(':urlFragment(attr)');
  // 
  // Arguments:
  // 
  //  attr - (String) Optional name of an attribute that will contain a URL to
  //    test fragment-ness against. See <jQuery.elemUrlAttr> for a list of
  //    default attributes.
  // 
  // Returns:
  // 
  //  (jQuery) A filtered jQuery collection of elements.
  
  function fn_selector( func, elem, i, match ) {
    var a = match[3] || jq_elemUrlAttr()[ ( elem.nodeName || '' ).toLowerCase() ] || '';
    
    return a ? !!func( elem.getAttribute( a ) ) : FALSE;
  };
  
  $.expr[':'][ str_urlInternal ] = curry( fn_selector, jq_isUrlInternal );
  $.expr[':'][ str_urlExternal ] = curry( fn_selector, jq_isUrlExternal );
  $.expr[':'][ str_urlFragment ] = curry( fn_selector, jq_isUrlFragment );
  
  // Section: Support methods
  // 
  // Method: jQuery.elemUrlAttr
  // 
  // Get the internal "Default URL attribute per tag" list, or augment the list
  // with additional tag-attribute pairs, in case the defaults are insufficient.
  // 
  // In the <jQuery.fn.urlInternal> and <jQuery.fn.urlExternal> methods, as well
  // as the <:urlInternal> and <:urlExternal> selectors, this list is used to
  // determine which attribute contains the URL to be modified, if an "attr"
  // param is not specified.
  // 
  // Default Tag-Attribute List:
  // 
  //  a      - href
  //  base   - href
  //  iframe - src
  //  img    - src
  //  input  - src
  //  form   - action
  //  link   - href
  //  script - src
  // 
  // Usage:
  // 
  // > jQuery.elemUrlAttr( [ tag_attr ] );
  // 
  // Arguments:
  // 
  //  tag_attr - (Object) An object containing a list of tag names and their
  //    associated default attribute names in the format { tag: 'attr', ... } to
  //    be merged into the internal tag-attribute list.
  // 
  // Returns:
  // 
  //  (Object) An object containing all stored tag-attribute values.
  
  // Only define function and set defaults if function doesn't already exist, as
  // the jQuery BBQ plugin will provide this method as well.
  $[ str_elemUrlAttr ] || ($[ str_elemUrlAttr ] = function( obj ) {
    return $.extend( elemUrlAttr_cache, obj );
  })({
    a: str_href,
    base: str_href,
    iframe: str_src,
    img: str_src,
    input: str_src,
    form: 'action',
    link: str_href,
    script: str_src
  });
  
  jq_elemUrlAttr = $[ str_elemUrlAttr ];
  
  // Method: jQuery.urlInternalHost
  // 
  // Constructs the regular expression that matches an absolute-but-internal
  // URL from the current page's protocol, hostname and port, allowing for any
  // number of optional hostnames. For example, if the current page is
  // http://benalman.com/test or http://www.benalman.com/test, specifying an
  // argument of "www" would yield this pattern:
  // 
  // > /^(?:http:)?\/\/(?:(?:www)\.)?benalman.com\//i
  // 
  // This pattern will match URLs beginning with both http://benalman.com/ and
  // http://www.benalman.com/. If the current page is http://benalman.com/test,
  // http://www.benalman.com/test or http://foo.benalman.com/test, specifying
  // arguments "www", "foo" would yield this pattern:
  // 
  // > /^(?:http:)?\/\/(?:(?:www|foo)\.)?benalman.com\//i
  // 
  // This pattern will match URLs beginning with http://benalman.com/,
  // http://www.benalman.com/ and http://foo.benalman.com/.
  // 
  // Not specifying any alt_hostname will disable any alt-hostname matching.
  // 
  // Note that the plugin is initialized by default to an alt_hostname of "www".
  // Should you need more control, <jQuery.urlInternalRegExp> may be used to
  // completely customize the absolute-but-internal matching pattern.
  // 
  // Usage:
  // 
  // > jQuery.urlInternalHost( [ alt_hostname [, alt_hostname ] ... ] );
  // 
  // Arguments:
  // 
  //  alt_hostname - (String) An optional alternate hostname to use when testing
  //    URL absolute-but-internal-ness. 
  // 
  // Returns:
  // 
  //  (RegExp) The absolute-but-internal pattern, as a RegExp.
  
  $.urlInternalHost = jq_urlInternalHost = function( alt_hostname ) {
    alt_hostname = alt_hostname
      ? '(?:(?:' + Array.prototype.join.call( arguments, '|' ) + ')\\.)?'
      : '';
    
    var re = new RegExp( '^' + alt_hostname + '(.*)', 'i' ),
      pattern = '^(?:' + loc.protocol + ')?//'
        + loc.hostname.replace(re, alt_hostname + '$1').replace( /\\?\./g, '\\.' )
        + (loc.port ? ':' + loc.port : '') + '/';
    
    return jq_urlInternalRegExp( pattern );
  };
    
  // Method: jQuery.urlInternalRegExp
  // 
  // Set or get the regular expression that matches an absolute-but-internal
  // URL.
  // 
  // Usage:
  // 
  // > jQuery.urlInternalRegExp( [ re ] );
  // 
  // Arguments:
  // 
  //  re - (String or RegExp) The regular expression pattern. If not passed,
  //    nothing is changed.
  // 
  // Returns:
  // 
  //  (RegExp) The absolute-but-internal pattern, as a RegExp.
  
  $.urlInternalRegExp = jq_urlInternalRegExp = function( re ) {
    if ( re ) {
      url_regexp = typeof re === 'string'
        ? new RegExp( re, 'i' )
        : re;
    }
    
    return url_regexp;
  };
  
  // Initialize url_regexp with a reasonable default.
  jq_urlInternalHost( 'www' );
  
})(jQuery);
