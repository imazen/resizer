var query = {};

//Splits the path and querystring apart, trimming the leading '?' or ';'. 
//Returns null when one part or the other isn't present.
query.split = function(url){

	var splitChar = url.length;
	//We accept both ';' and '?' querystring formats (Amazon CloudFront compatibility)
	//Chose the first instance as the querystring indicator.
	var q = url.indexOf('?');
	var s = url.indexOf(';');
	if ((q < s || s < 0) && q > -1) splitChar = q;
	else if ((s < q || q < 0) && s > -1) splitChar = s;
	
	//If the URL has '&' in it, but no '/', assume it is a querystring
	if (url.indexOf('/') < 0 && url.indexOf('&') > -1) return {path:null,query:url};
	//Otherwise return the 'url' as the path if there was no splitChar
	else return {path:splitChar > 0 ? url.substr(0,splitChar) : null, query:splitChar < url.length - 1 ? url.substring(splitChar + 1) : null};
}

query.get = function(url){

	url = query.split(url);
	//If no inicator is found, assume it is all a querystring - unless there is a / indicating it is a URL.
	if (url.query == null && url.path.indexOf('/') < 0) url.query = url.path;
	
	var urlParams = {};
	(function () {
	    var e,
	        a = /\+/g,  // Regex for replacing addition symbol with a space
	        r = /([^&;=]+)=?([^&;]*)/g,
	        d = function (s) { return decodeURIComponent(s.replace(a, " ")); },
	        q = url.query;

	    while (e = r.exec(q))
	       urlParams[d(e[1])] = d(e[2]);
	})();
	return urlParams;
};
//Returns null if no path segment exists
query.getpath = function(url){
	return query.split(url).path;
}

query.cloudFront = false;

query.set = function (path, queryobj){
	path = query.split(path).path; //Remove the old querystring
	if (query.cloudFront) path += ";"; else path += "?"; //Add the appropriate delimiter
	var isFirst = true; //For tracking when we need to add a delimter
	for(var key in queryobj){
		if (key == null) continue;
		if (queryobj[key] == null) continue;
		
		path += ((!isFirst) ? (query.cloudFront ? ";" : "&") : "") + encodeURIComponent(key) + "=" + encodeURIComponent(queryobj[key]);
		isFirst = false;
	}
	return path;
};



(function( $ ){

  $.fn.jimage = function( options ) {  

    var settings = {
      'location'         : 'top',
      'background-color' : 'blue'
    };

		var menuItems = [];
		
		menuItems.push({title:"Edit",key:"edit", handler:function(el){
			var d= $('<div></div>').addClass('jqmWindow')
					.html('<a href="#" class="jqmClose">Close</a>').appendTo(document.body);


					
			var oldUrl = $(el).attr('src');
			var imgMaxWidth = Math.round($(window).width() * 0.98 - 400);
			var imgMaxHeight = Math.round($(window).height() * 0.98);
			
			var q = query.get(oldUrl);
			q['width'] = null;
			q['height'] = null;
			q['maxwidth'] = imgMaxWidth;
			q['maxheight'] = imgMaxHeight;
			
			var url = query.set(oldUrl,q);
			
			$("<img />").attr('class','editing').css('float','left').attr('src',url).appendTo(d);
			
			var accordion = $("<div />").attr('class','tools').css('width','400px').css('float','right').appendTo(d);
			
			accordion.append("<h3><a href='#'>Crop</a></h3>");
			accordion.append("<div> Preserve aspect ratio</div>");
			accordion.append("<h3><a href='#'>Resize</a></h3>");
			accordion.append("<div> Set dimensions, enable upscaling</div>");
			
			accordion.append("<h3><a href='#'>Borders & Padding</a></h3>");
			accordion.append("<div>Adjust margins, borders, and padding</div>");
			
			accordion.append("<h3><a href='#'>Compression and format</a></h3>");
			accordion.append("<div>Adjust margins, borders, and padding</div>");
			
			accordion.append("<h3><a href='#'>Adjust & Filter</a></h3>");
			accordion.append("<div>Adjust brightness, saturation, contrast, alpha, and apply filters</div>");
			

			
			accordion.append("<h3><a href='#'>Effects</a></h3>");
			accordion.append("<div>Adjust margins, borders, and padding</div>");
			
			accordion.append("<h3><a href='#'>Watermark</a></h3>");
			accordion.append("<div>Overlay logos or transparencies</div>");
			
			
			
			accordion.accordion();
			
			d.jqm().jqmShow();
			
		}});
		
		menuItems.push({title:"Open in new tab",key:"newtab", handler:function(el){
			alert(el.attr('src'));
			window.open(el.attr('src'),"newtab");
		}});
		

		var menuOptions 



		var menu = $("<ul class='contextMenu'/>");
		
		for(var i = 0; i < menuItems.length; i++)
			$("<li class='" + menuItems[i].key + "' />").append($("<a href='#" + menuItems[i].key + "'>" + menuItems[i].title + "</a>")).appendTo(menu);
		var handler = function(action, el, pos) {
			for(var i = 0; i < menuItems.length; i++){
				if (menuItems[i].key == action) {
					menuItems[i].handler(el);
					return;
				}
			}
		};
		
		menu.appendTo(document.body);

    return this.each(function() {
	        
			var $this = $(this);
      // If options exist, lets merge them
      // with our default settings
      if ( options ) { 
        $.extend( settings, options );
      }

      //Plugin code here
			$this.contextMenu({ menu: menu},handler);
    });

  };
})( jQuery );