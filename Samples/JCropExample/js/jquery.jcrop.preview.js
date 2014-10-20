(function ($) {
    $.fn.JcropPreview = function (options) {

        var defaults = {
            jcropImg: null,
            defaultWidth: 100,
            defaultHeight: 100
        };

        var options = $.extend(defaults, options);
        options.jcropImg = $(options.jcropImg);

        return this.each(function () {
            var obj = $(this);


            //Clear all previous contents
            obj.empty();

            //Allow the div to override the default width and height in the style attribute
            var previewMaxWidth = (obj.attr('style') != null && obj.attr('style').indexOf('width') > -1) ? obj.width() : options.defaultWidth;
            var previewMaxHeight = (obj.attr('style') != null && obj.attr('style').indexOf('height') > -1) ? obj.height() : options.defaultHeight;
            //Set the values explicitly.
            obj.css({
                width: previewMaxWidth + 'px',
                height: previewMaxHeight + 'px',
                overflow: 'hidden'
            });

            //Create another child div and style it to form a 'clipping rectangle' for the preview div.
            var innerPreview = $('<div />').css({
                overflow: 'hidden'
            }).addClass('innerPreview').appendTo(obj);


            //Create a copy of the image inside the inner preview div(s)
            var innerImg = $('<img />').attr('src', options.jcropImg.attr('src')).appendTo(innerPreview);


            var update = function (coords) {
                //Require valid width and height to do anything
                if (parseInt(coords.w) <= 0 || parseInt(coords.h) <= 0) return; //Require valid width and height
                //Resolve JCrop image target to jCrop API reference.
                if (options.jcropRef == null) options.jcropRef = $(options.jcropImg).data('Jcrop');

                var imgSize = options.jcropRef.getWidgetSize();
                var jopts = options.jcropRef.getOptions;

                //The aspect ratio of the cropping rectangle.
                var cropRatio = coords.w / coords.h;
                // Used forced ratio if present, as it is more precise and fixes jitter
                if (jopts != null && jopts().aspectRatio) cropRatio = jopts().aspectRatio;

                //When the selection aspect ratio changes, the preview clipping area has to also.
                //Calculate the width and height.
                var innerWidth = cropRatio >= (previewMaxWidth / previewMaxHeight) ? previewMaxWidth : previewMaxHeight * cropRatio;
                var innerHeight = cropRatio < (previewMaxWidth / previewMaxHeight) ? previewMaxHeight : previewMaxWidth / cropRatio;

                innerPreview.css({
                    width: Math.ceil(innerWidth) + 'px',
                    height: Math.ceil(innerHeight) + 'px',
                    marginTop: (previewMaxHeight - innerHeight) / 2 + 'px',
                    marginLeft: (previewMaxWidth - innerWidth) / 2 + 'px',
                    overflow: 'hidden'
                });
                //Set the outer div's padding so it stays centered
                obj.css({

                });

                //Calculate how much we are shrinking the image inside the preview window
                var scalex = innerWidth / coords.w;
                var scaley = innerHeight / coords.h;

                //Set the width and height of the image so the right areas appear at the right scale appear.
                innerImg.css({
                    width: Math.round(scalex * imgSize[0]) + 'px',
                    height: Math.round(scaley * imgSize[1]) + 'px',
                    marginLeft: '-' + Math.round(scalex * coords.x) + 'px',
                    marginTop: '-' + Math.round(scaley * coords.y) + 'px'
                });
            };

            obj.data('updateFunc', update);
        });
    };

    $.fn.JcropPreviewUpdate = function (coords) {
        return this.each(function () {
            $(this).data('updateFunc')(coords);
        });
    };
    $.fn.JcropPreviewUpdateFn = function () {
        var t = $(this);
        return function(coords) {
            t.data('updateFunc')(coords);
        };
    };

})(jQuery);  



	            

	            


