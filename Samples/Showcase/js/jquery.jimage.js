(function( $ ){

  $.fn.jimage = function( options ) {  

    var settings = {
      'location'         : 'top',
      'background-color' : 'blue'
    };

		var menuItems = [];
		
		menuItems.push({title:"Crop",key:"crop", handler:function(el){
			
		}});
		
		menuItems.push({title:"Open in new tab",key:"newtab", handler:function(el){
			window.open(el.src,"newtab");
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