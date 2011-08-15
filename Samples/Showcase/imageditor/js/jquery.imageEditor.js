/*
 * Copyright (c) 2010 Carlo Roosen (http://www.carloroosen.nl)
 * Dual licensed under the MIT (http://www.opensource.org/licenses/mit-license.php) 
 * and GPL (http://www.opensource.org/licenses/gpl-license.php) licenses.
 */

(function($) {
	jQuery.fn.imageEditor = function (settings) {
		
		// Commands in jQuery UI style. can be called like $().imageEditor('result')
		if (typeof(settings)=='string') {
			if (settings == 'shout') {
				alert('AAAARCHH!!!!!!!');
				return ;
			}	
		}

		// Create DOM
		return this.each(function(){
			if ($(this).is('img')) {
				var image = $(this);
				image.wrap('<div class="image-editor-container">');
				var container = image.parent();
				container.css({
					'width': image.css('width'),
					'height': image.css('height'),
					'min-width': image.css('min-width'),
					'min-height': image.css('min-height'),
					'max-width': image.css('max-width'),
					'max-height': image.css('max-height')
				})
			} else {
				var container=$(this);
				container.addClass('image-editor-container');
				var image = container.children('img');
			}
			image.after('<div class="image-editor-slider" />');
			var slider = container.children('.image-editor-slider');
			slider.wrap('<div style="padding:10px 16px"/>');

			// retrieve sizes from image, then delete css ant attributes to retrieve source width and height
			var scaledImageW = image.width();
			var scaledImageH = image.height();
			image.css({
				'width':'auto',
				'height':'auto'
			});
			image.removeAttr('width'); 
			image.removeAttr('height');
			// get attributes
			// retrieve sizes from css
			var attributes = jQuery.extend({
				scaleType :'fill',
				imageMaxW: image.width(),
				imageMaxH: image.height(),
				scaledImageW: scaledImageW,
				scaledImageH: scaledImageH,
				preferredContainerW: container.width(),
				preferredContainerH: container.height(),
				containerW: container.width(),
				containerH: container.height(),
				relativeX: ((container.width() - scaledImageW != 0) ? parseInt(image.css('left')) / (container.width() - scaledImageW) : 1 ),
				relativeY: ((container.height() - scaledImageH != 0) ? parseInt(image.css('top')) / (container.height() - scaledImageH) : 1 ),
				scale : (image.width() > 0 ? scaledImageW / image.width() : 1),
				handles: 'e, s, se',
				containerMinW: (parseInt(container.css('min-width')) > 0 ? parseInt(container.css('min-width')) : container.width()),
				containerMinH: (parseInt(container.css('min-height')) > 0 ? parseInt(container.css('min-height')) : container.height()),
				containerMaxW: ((container.css('max-width') != 'none') ? parseInt(container.css('max-width')) : container.width()),
				containerMaxH: ((container.css('max-height') != 'none') ? parseInt(container.css('max-height')) : container.height()),
				autoHide : true
			}, settings );

			var preferredContainerW = attributes.preferredContainerW;
			var preferredContainerH = attributes.preferredContainerH; 
			var containerMinW = attributes.containerMinW;
			var containerMinH = attributes.containerMinH;
			var containerMaxW = attributes.containerMaxW;
			var containerMaxH = attributes.containerMaxH;
			var scaledImageW = attributes.scaledImageW;
			var scaledImageH = attributes.scaledImageH;
			var relativeX = attributes.relativeX;
			var relativeY = attributes.relativeY;
			var containerW = attributes.containerW;
			var containerH = attributes.containerH;
			var preferredScale = attributes.scale;
			var minX;
			var minY;
			var maxX;
			var maxY;
			var imageX;
			var imageY;
			
			if (attributes.scaleType !='fit' ) {
				containerMaxW = Math.min(containerMaxW, attributes.imageMaxW)
				containerMaxH = Math.min(containerMaxH, attributes.imageMaxH)
			}	
			
			// Modify css (functional requirements)
			image.css({
				'position':'absolute',
				'width':'auto',
				'height':'auto',
				'cursor':'move',
				'min-width': 0,
				'min-height': 0,
				'max-width': 'none',
				'max-height': 'none',
				'top':0,
				'left':0
			});
			container.css({
				'overflow':'hidden'
			});

			// Container
			container.resizable({
				resize: function(event, ui) {
					preferredContainerW = ui.size.width;
					preferredContainerH = ui.size.height;
					setScaleConstraints();
					setScale();
					setSize();
					setSlider();
					setScalable();
					setDraggable();
				},
				stop: function(event, ui) {
					setPosition();
					setData();
				},
				handles: attributes.handles
			});

			// set constraints
			container.resizable("option", "maxWidth", containerMaxW );
			container.resizable("option", "maxHeight", containerMaxH );
			container.resizable("option", "minWidth", containerMinW );
			container.resizable("option", "minHeight", containerMinH );

			
			// Image	
			image.draggable({
				drag: function(event, ui) {
					if (ui.position.left < minX) ui.position.left=minX;
					if (ui.position.left > maxX) ui.position.left=maxX;
					if (ui.position.top < minY) ui.position.top=minY;
					if (ui.position.top > maxY) ui.position.top=maxY;
				},
				stop: function(event,ui) {
					imageX = ui.position.left;
					imageY = ui.position.top;
					relativeX = imageX / (containerW - (scale * attributes.imageMaxW));
					relativeY = imageY / (containerH - (scale * attributes.imageMaxH));
					setData();
				}
			});
			
			// Slider
			slider.slider({
				values: [0],
				max:1000,
				start: function(event, ui) {
					storedContainerW = preferredContainerW;
					storedContainerH = preferredContainerH;
				},
				slide: function(event, ui) { 
					preferredScale = sliderOffset + ((1-sliderOffset) * ui.value/1000);  
					if (attributes.scaleType =='fit' ) {
						preferredContainerW = attributes.imageMaxW * preferredScale;
						preferredContainerH = attributes.imageMaxH * preferredScale;
						setScaleConstraints();
						setScale();
					} else {	
						scaleMin= Math.min(1, Math.max(containerMinH/attributes.imageMaxH,containerMinW/attributes.imageMaxW));
						scale = Math.max(scaleMin,Math.min(preferredScale, 1));
						preferredContainerW = storedContainerW;
						preferredContainerH = storedContainerH;
						scaledImageW = attributes.imageMaxW * scale;
						scaledImageH = attributes.imageMaxH * scale;
					}
					setSize();
					setResizable();
					setScalable();
					setDraggable();
				},
				stop: function(event, ui) {
					setPosition();
					setData()
					
					// current scale is sticky
					preferredScale=scale;
				}
			});

			// init All
			var sliderOffset;
			if (attributes.scaleType=='fit') {
				sliderOffset = Math.min(containerMinW/attributes.imageMaxW,containerMinH/attributes.imageMaxH); 
			} else {
				sliderOffset = Math.max(containerMinW/attributes.imageMaxW,containerMinH/attributes.imageMaxH); 
			}	
			setScaleConstraints();
			setScale();
			setSize();
			setResizable();
			setPosition();
			setSlider();
			setScalable();
			setDraggable ();
			setData();
			if (attributes.autoHide) {
				var c = container.children().not('img').hide();
				container.hover(
					function() {
						c.stop(true,true).fadeIn(300);
					},
					function() {
						c.stop(true,true).delay(2000).fadeOut(300);
					}
				);	
			}

			
			function setScaleConstraints() {
				preferredContainerW = Math.min(preferredContainerW, containerMaxW);
				preferredContainerH = Math.min(preferredContainerH, containerMaxH);
				if (attributes.scaleType=='fit') {
					scaleMin= Math.min(1, Math.min(preferredContainerH/attributes.imageMaxH,preferredContainerW/attributes.imageMaxW));
				} else {
					scaleMin= Math.min(1, Math.max(preferredContainerH/attributes.imageMaxH,preferredContainerW/attributes.imageMaxW));
				}

			}
			
			function setScale() {
				scale = Math.max(scaleMin,Math.min(preferredScale, 1));
				scaledImageW = attributes.imageMaxW * scale;
				scaledImageH = attributes.imageMaxH * scale;
			}

			function setSize() {
				// the actual container size is constrained by 4 values in both directions. Here the priority is defined.
				if (attributes.scaleType =='fit' ) {
					containerW = Math.max( Math.min( preferredContainerW , containerMaxW), containerMinW);
					containerH =  Math.max( Math.min( preferredContainerH , containerMaxH), containerMinH);
				} else {	
					containerW =  Math.min( Math.max( Math.min( Math.min(preferredContainerW , attributes.imageMaxW), containerMaxW), containerMinW), scaledImageW);
					containerH =  Math.min( Math.max( Math.min( Math.min(preferredContainerH , attributes.imageMaxH), containerMaxH), containerMinH), scaledImageH);
				}		
			}
			
			function setPosition() {
				// ui position bounderies
				minX = Math.min(containerW - scaledImageW, 0);
				minY = Math.min(containerH - scaledImageH, 0);
				maxX = Math.max(containerW - scaledImageW, 0);
				maxY = Math.max(containerH - scaledImageH, 0);
			}
			
			function setSlider() {
				sliderValue = 1000 * (scale - sliderOffset) / (1-sliderOffset); 
				slider.slider( "values", 0 , sliderValue );
			}
			
			function setScalable () {
				image.css('width', scaledImageW);
				image.css('height', scaledImageH);
			}	
			
			function setResizable () {
				container.css('width',containerW);
				container.css('height',containerH);
			}	
			
			function setDraggable () {
				imageX = (containerW - scaledImageW) * relativeX;
				imageY = (containerH - scaledImageH) * relativeY;
				image.css('left',imageX+'px');
				image.css('top',imageY+'px');
			}	
			
			function setData () {
				jQuery.extend(image.data(), {
					sourceWidth : attributes.imageMaxW,
					sourceHeight : attributes.imageMaxH,
					scale : scale,
					scaledWidth : scaledImageW,
					scaledHeight : scaledImageH,
					resultWidth : containerW,
					resultHeight : containerH,
					left : imageX,
					right : containerW - scaledImageW - imageX,
					top : imageY,
					bottom : containerH - scaledImageH - imageY,
					relativeX: relativeX,
					relativeY: relativeY,
					cropLeft : Math.round(relativeX * (attributes.imageMaxW - containerW / scale)),
					cropRight : Math.round((1 - relativeX) * (attributes.imageMaxW - containerW / scale)),
					cropTop : Math.round(relativeY * (attributes.imageMaxH - containerH / scale)),
					cropBottom : Math.round((1 - relativeY) * (attributes.imageMaxH - containerH / scale)),
					scaleType : attributes.scaleType
				});	
			}
		});
    return this;
	};
})(jQuery);
