<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Advanced.aspx.cs" Inherits="JCropExample.Advanced" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">

    <title>Combining jCrop with server-side resizing and cropping</title>

    <script src="js/jquery.min.js" type="text/javascript"></script> 
	<script src="js/jquery.Jcrop.js" type="text/javascript"></script> 
	<link rel="stylesheet" href="css/jquery.Jcrop.css" type="text/css" /> 
 
	<script  type="text/javascript">

	    function linkUp(unusedIndex, container) {
	        container = $(container); //We were passed a DOM reference, convert it to a jquery object

	        //The code will look for a img.image, a div.preview, a.result, an input.result inside the specified container, and link them together.
	        //Only 'img.image' is required, however.  


	        //Find the original image
	        var image = container.find("img.image");

	        //Find the original aspect ratio of the image
	        var originalRatio = image.width() / image.height()

	        //Defaults to false if a checkbox with class="keepAspectRatio" doesn't exist.
	        var keepRatio = container.find('.keepAspectRatio').size() < 1 ? false : container.find('.keepAspectRatio').is(':checked');

	        //jCrop will enforce this ratio:
	        var forcedRatio = keepRatio ? originalRatio : null;

	        //Find the URL of the original image minus the querystring.
	        var path = image.attr('src');
	        if (path.indexOf('?') > 0) path = path.substr(0, path.indexOf('?'));
	        if (path.indexOf(';') > 0) path = path.substr(0, path.indexOf(';')); //For parsing Amazon-cloudfront compatible querystrings

	        var cloudFront = image.attr('src').indexOf(';') > -1; //To use CloudFront-friendly URLs.

	        //Find the preview div(s) (if they exist) and make sure the have a set height and width.
	        var divPreview = container.find("div.preview");

	        //What size to make the preview window (defaults to existing width/height if specified in 'style' attribute)
	        var previewFallbackWidth = 100;
	        var previewFallbackHeight = 100;

	        //Allow the div to override the default width and height in the style attribute
	        var previewMaxWidth = (divPreview.attr('style') != null && divPreview.attr('style').indexOf('width') > -1) ? divPreview.width() : previewFallbackWidth;
	        var previewMaxHeight = (divPreview.attr('style') != null && divPreview.attr('style').indexOf('height') > -1) ? divPreview.height() : previewFallbackHeight;
	        //Set the values explicitly.
	        divPreview.css({
	            width: previewMaxWidth + 'px',
	            height: previewMaxHeight + 'px',
	            overflow: 'hidden'
	        });

	        //Create another child div and style it to form a 'clipping rectangle' for the preview div.
	        var innerPreview = $('<div />').css({
	            overflow: 'hidden'
	        }).addClass('innerPreview').appendTo(divPreview);

	        //Create a copy of the image inside the inner preview div(s)
	        $('<img />').attr('src', image.attr('src')).appendTo(innerPreview);

	        //Find any links (if they exist)
	        var links = container.find('a.result');
	        //And any input fields (for postbacks, if desired)
	        var inputs = container.find('input.result');


	        //Create a function to update the link, hidden input, and preview pane
	        var update = function (coords) {
	            if (parseInt(coords.w) <= 0 || parseInt(coords.h) <= 0) return; //Require valid width and height

	            //The aspect ratio of the cropping rectangle. If 'keepRatio', use originalRatio since it's more precise.
	            var cropRatio = keepRatio ? originalRatio : (coords.w / coords.h);


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
	            divPreview.css({

	            });



	            //Calculate how much we are shrinking the image inside the preview window
	            var scalex = innerWidth / coords.w;
	            var scaley = innerHeight / coords.h;

	            //Set the width and height of the image so the right areas appear at the right scale appear.
	            innerPreview.find('img').css({
	                width: Math.round(scalex * image.width()) + 'px',
	                height: Math.round(scaley * image.height()) + 'px',
	                marginLeft: '-' + Math.round(scalex * coords.x) + 'px',
	                marginTop: '-' + Math.round(scaley * coords.y) + 'px'
	            });



	            //Calculate the querystring
	            var query = '?';

	            //Add final size, if specified.
	            var inputWidth = container.find('input.width');
	            var inputHeight = container.find('input.height');
	            if (inputWidth.size() > 0 && parseInt(inputWidth.val()) > 1) query += 'maxwidth=' + inputWidth.val() + '&';
	            if (inputHeight.size() > 0 && parseInt(inputHeight.val()) > 1) query += 'maxheight=' + inputHeight.val() + '&';

	            //Add crop rectangle
	            query += 'crop=(' + coords.x + ',' + coords.y + ',' + coords.x2 + ',' + coords.y2 + ')&cropxunits=' + image.width() + '&cropyunits=' + image.height()
	            //Replace ? and & with ; if using Amazon Cloudfront
	            if (cloudFront) query = query.replace(/\?\&/g, ';');

	            //Now, update the links and input values.
	            links.attr('href', path + query);
	            inputs.attr('value', path + query);

	        }

	        //Start up jCrop
	        var jcrop_reference = $.Jcrop(image);
	        jcrop_reference.setOptions({
	            onChange: update,
	            onSelect: update,
	            aspectRatio: forcedRatio,
	            bgColor: 'black',
	            bgOpacity: 0.6
	        });

	        //Call the function to init the preview windows
	        update({ x: 0, y: 0, x2: image.width(), y2: image.height(), w: image.width(), h: image.height() });

	        //Handle the 'lock ratio' checkbox change vent
	        container.find('.keepAspectRatio').change(function (e) {
	            //Update keepRatio value
	            keepRatio = this.checked;

	            //Update the forcedRatio value
	            forcedRatio = keepRatio ? originalRatio : null;

	            //Update the jcrop settings
	            jcrop_reference.setOptions({ aspectRatio: forcedRatio });
	            jcrop_reference.focus();
	        });

	    }

	    // Remember to invoke within jQuery(window).load(...)
	    // If you don't, Jcrop may not initialize properly
	    jQuery(window).load(function () {
	        $('.image-cropper').each(linkUp);
	    });

	
	</script>
    <style type="text/css">
    .jcropper-holder { border: 1px black solid; }

    </style> 
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <h1>Combining jCrop with server-side resizing and cropping</h1>
        <div class="image-cropper">
            <table>
                <tr>
                    <td>
                        <img src="fountain-small.jpg?width=400" runat="server" class="image" />
                    </td>
                    <td>
                        <div style="margin:50px; width:200px; text-align:center">
                            <div class="preview" style="width:200px;height:200px">
                            </div>
                            <input type="checkbox" class="keepAspectRatio" /> Lock aspect ratio<br />
                            
                            Max width:<input type="text" class="width" value="800" /><br />
                            Max height:<input type="text" class="height" value="600" /><br />
                            <a class="result" href="#">View the result</a><br />
                            <input id='img1' type="hidden" class="result" value="" runat="server" />
                            <input type="submit" value="Send result to server" />
                        </div>
                    </td>
                </tr>
            </table>
        </div>

        <h1>Client-side only</h1>
        <div class="image-cropper" style="width:300px; text-align:center">
            
            <img src="fountain-small.jpg?width=300" runat="server" class="image" />
            <div class="preview" style="margin-left:100px">
            </div>
            <a class="result" href="#">View result</a>
        </div>
        <h1>Without a preview</h1>
        <div class="image-cropper" style="width:300px; text-align:center">
            
            <img src="red-leaf.jpg?width=300" runat="server" class="image" />
            <a class="result" href="#">View result</a>
        </div>
    </div>
    </form>
</body>
</html>