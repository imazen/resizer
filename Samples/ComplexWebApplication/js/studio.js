//Parse a URL into path, querystring, and querystring object
var parse = function(url){
    var path = url;
    var qs = "";
    var splitAt = path.indexOf('?');
    if (splitAt > 0) {
        qs = path.substr(splitAt + 1);
        path = path.substr(0, splitAt);
    }
    var obj = QueryString.parseQuery(qs);
    for (var key in obj){
        var val = obj[key];
        delete obj[key];
        obj[key.toLowerCase()] = val;
    }
    return {
        obj: obj,
        query: qs,
        path:path
        };
};
var edit = function (img, callback) {
    var data = parse(img.attr('src'));
    img.data('obj',  data.obj); 
    callback(data.obj);
    img.attr('src', data.path + '?' + QueryString.stringify(data.obj));
    img.triggerHandler('query', [data.obj]);
};
var setUrl = function(img, url, silent){
    var data = parse(url);
    img.data('obj',  data.obj); 
    img.attr('src', url);
    if (!silent) img.triggerHandler('query', [data.obj]);
}
var getObj = function(img){
    if (!img.data('obj'))  img.data('obj',parse(img.attr('src')).obj);
    return img.data('obj');
}
var getValue = function (img, key) {
    if (!img.data('obj'))  img.data('obj',parse(img.attr('src')).obj);
    return img.data('obj')[key];
}
var toBool = function (val) {
    if (val === null || val === undefined) return false;
    if (val == "false") return false;
    if (val == "0") return false;
    if (val == "no") return false;
    return true;
}
var parseFlip = function (value) {
    if (value == null) return { x: 0, y: 0 };
    value = value.toString().toLowerCase();
    var split = { "both": [1, 1], "xy": [1, 1], "x": [1, 0], "y": [0, 1], "h": [1, 0], "v": [0, 1], "none": [0, 0] };
    return { x: split[value][0], y: split[value][1] };
}
var toggleFlip = function (originalValue, togglex, toggley) {
    var restore = { "11": "xy", "10": "x", "01": "y", "00": "none" };
    var flip = parseFlip(originalValue);
    flip.x = flip.x ^ togglex;
    flip.y = flip.y ^ toggley;
    return restore[flip.x.toString() + flip.y.toString()];
}
//Makes a button that edits the image's querystring.
var makeButton = function (img, text, icon, editCallback, clickCallback) {
    var b = $('<button type="button"></button>').button({ label: text, icons: icon != null ? { primary: "ui-icon-" + icon} : {}
    });
    if (editCallback) b.click(function () {
        edit(img, function (obj) {
            editCallback(obj);
        });
    });

    if (clickCallback) b.click(clickCallback);
    return b;
};
var addToggleButton = function (img, container, text, icon, querystringKey) {
    if (!window.uniqueId) window.uniqueId = (new Date()).getTime();
    window.uniqueId++;
    var chk = $('<input type="checkbox" id="' + window.uniqueId + '" />');
    chk.prop("checked", toBool(getValue(img, querystringKey)));
    chk.appendTo(container);
    $('<label for="' + window.uniqueId + '">' + text + '</label>').appendTo(container);
    chk.button({ icons: { primary: "ui-icon-" + icon} }).click(function () {
        edit(img, function (obj) {
            obj[querystringKey] = !toBool(obj[querystringKey]);
        });
    });
    img.bind('query', function (e, obj) {
        var b = toBool(obj[querystringKey]);
        if (chk.prop("checked") != b) chk.prop("checked", b);
        chk.button('refresh');
    });
};
var slider = function (img, min, max,  step, key) {
    var supress = {};
    var startingValue = getValue(img, key); if (startingValue == null) startingValue = 0;
    var s = $("<div></div>").slider({ min: min, max: max, step:step, value: startingValue,
        change: function (event, ui) {
            supress[key] = true;
            edit(img, function (obj) {
                obj[key] = ui.value;
            });
            supress[key] = false;
        }
    });
    img.bind('query', function (e, obj) {
        if (supress[key]) return;
        var v = obj[key]; if (v == null) v = 0;
        if (v != s.slider('value')) {
            s.slider('value', v);
        }
    });
    return s;
};

//Adds a pane for rotating and flipping the source image
var addRotateFlipPane = function (img, accordion) {
    accordion.append('<h3><a href="#">Rotate &amp; Flip</a></h3>');
    var c = $('<div></div>');
    makeButton(img, "Rotate left", "arrowreturnthick-1-w", function (obj) {
        obj["srotate"] = obj["srotate"] == null ? -90 : (obj["srotate"] - 90) % 360;
    }).appendTo(c);
    makeButton(img, "Rotate right", "arrowreturnthick-1-e", function (obj) {
        obj["srotate"] = obj["srotate"] == null ? 90 : (obj["srotate"] + 90 % 360)
    }).appendTo(c);
    makeButton(img, "Flip vertical", "arrowthick-2-n-s", function (obj) {
        obj["sflip"] = toggleFlip(obj["sflip"], false, true);
    }).appendTo(c);
    makeButton(img, "Mirror", "arrowthick-2-e-w", function (obj) {
        obj["sflip"] = toggleFlip(obj["sflip"], true, false);
    }).appendTo(c);
    var lRot = $("<h3></h3>").appendTo(c);

    var updateLabels = function (e,obj) {
        var f = parseFlip(obj["sflip"]);
        lRot.text("Image rotated " + (!obj["srotate"] ? 0 : (obj["srotate"] % 360)) + " degrees and " + 
         (f.x ? ("flipped horizontally " + (f.y ? " and vertically" : "")) : (f.y ? "flipped vertically" : "not flipped")));
    }
    updateLabels(null, getObj(img));
    img.bind('query', updateLabels);
    accordion.append(c);
};

var addAdjustPane = function (img, accordion, options) {
    accordion.append('<h3><a href="#">Adjust image</a></h3>');
    var c = $('<div></div>');

    addToggleButton(img, c, "Auto-fix", "image", "a.equalize");

    c.append("<h3>Contrast</h3>");
    c.append(slider(img,-1,1,0.001,"s.contrast"));
    c.append("<h3>Saturation</h3>");
    c.append(slider(img, -1, 1, 0.001, "s.saturation"));
    c.append("<h3>Brightness</h3>");
    c.append(slider(img, -1, 1,  0.001, "s.brightness"));
    makeButton(img, "Reset", "cancel", function (obj) {
        delete obj["s.contrast"];
        delete obj["s.saturation"];
        delete obj["s.brightness"];
        delete obj["a.equalize"];
    }).appendTo(c);



    accordion.append(c);
};

var addEffectsPane = function (img, accordion, options) {
    accordion.append('<h3><a href="#">Effects &amp; Filters</a></h3>');
    var c = $('<div></div>');

    addToggleButton(img, c, "Black & White", "image", "s.grayscale");
    addToggleButton(img, c, "Sepia", "image", "s.sepia");
    addToggleButton(img, c, "Negative", "image", "s.invert");

    
    c.append("<h3>Smart Sharpen</h3>");
    c.append(slider(img, 0, 15, 1, "a.sharpen"));
    c.append("<h3>Noise Removal</h3>");
    c.append(slider(img, 0, 100, 1, "a.removenoise"));
    c.append("<h3>Oil painting</h3>");
    c.append(slider(img, 0, 25, 1, "a.oilpainting"));
    c.append("<h3>Posterize</h3>");
    c.append(slider(img, 0, 255, 1, "a.posterize"));
    c.append("<h3>Gaussian Blur</h3>");
    c.append(slider(img, 0, 40, 1, "a.blur"));


    accordion.append(c);
};

var addRedEyePane = function (img, accordion, options) {
    accordion.append('<h3><a href="#">Red-eye removal</a></h3>');
    var c = $('<div></div>');
    addToggleButton(img, c, "Auto-fix", "search", "r.auto");

    makeButton(img, "Select eyes", "pencil", function (obj) {

    }).appendTo(c);

    makeButton(img, "Done", null, function (obj) {

    }).appendTo(c);

    makeButton(img, "Cancel", null, function (obj) {

    }).appendTo(c);

    makeButton(img, "Reset All", null, function (obj) {

    }).appendTo(c);

    accordion.append(c);
};

var addCarvePane = function (img, accordion, options) {
    accordion.append('<h3><a href="#">Object removal</a></h3>');
    var c = $('<div></div>');

    makeButton(img, "Remove objects", null, function (obj) {

    }).appendTo(c);
    
    makeButton(img, "Mark areas to remove", "pencil", function (obj) {

    }).appendTo(c);

    makeButton(img, "Mark areas to preserve", null, function (obj) {

    }).appendTo(c);
    c.append("<h3>Brush size</h3>");
    $("<div></div>").slider({ min: 0, max: 15, value: 0,
        change: function (event, ui) {
            edit(img, function (obj) {
                obj["a.sharpen"] = ui.value;
            });
        }
    }).appendTo(c);

    makeButton(img, "Clear All", null, function (obj) {

    }).appendTo(c);

    makeButton(img, "Done", null, function (obj) {

    }).appendTo(c);

    accordion.append(c);
};
//Adds a pane for cropping
var addCropPane = function (img, accordion, options) {
    accordion.append('<h3><a href="#">Crop</a></h3>');
    var c = $('<div></div>');

    var cropping = false;
    var jcrop_reference
    var previousUrl = null;

    var startCrop = function (uncroppedWidth, uncroppedHeight, uncroppedUrl) {
        cropping = true;

        btnCrop.hide();
        //Prevent the accordion from changing, but don't gray out this panel
        accordion.accordion("disable");
        c.removeClass("ui-state-disabled");
        c.removeClass("ui-accordion-disabled");
        accordion.removeClass("ui-state-disabled");


        //Get the original crop values and URL
        var obj = getObj(img);
        previousUrl = img.attr('src');

        //Switched to uncropped image
        img.attr('src', uncroppedUrl);
        img.data('obj', null);

        //Start jcrop
       

        //Use existing coords if present, otherwise select entire image
        var coords = [0, 0, uncroppedWidth, uncroppedHeight];
        if (obj["crop"] && obj["cropxunits"] && obj["cropyunits"]) {
            var xfactor = uncroppedWidth / obj["cropxunits"];
            var yfactor = uncroppedHeight / obj["cropyunits"];
            coords[0] *= xfactor;
            coords[2] *= xfactor;
            coords[1] *= yfactor;
            coords[3] *= yfactor;
        }

        preview.JcropPreview({ jcropImg: img });

        //Start up jCrop
        img.Jcrop({
            onChange: preview.JcropPreviewUpdateFn(),
            onSelect: preview.JcropPreviewUpdateFn(),
            aspectRatio: getRatio(),
            bgColor: 'black',
            bgOpacity: 0.6
        }, function () {
            jcrop_reference = this;
            this.setSelect(coords);

            
            //preview.JcropPreviewUpdateFn()({ x: coords[0], y: coords[1], x2: coords[2], y2: coords[3], w: coords[2] - coords[0], h: coords[3] - coords[1] });

            //Show buttons
            btnCancel.show();
            btnDone.show();
            preview.show();
            ratio.show();

        });

    }


    var stopCrop = function (save) {
        cropping = false;
        if (save) {
            setUrl(img, previousUrl, true);
            var coords = jcrop_reference.tellSelect();
            edit(img, function (obj) {
                obj['crop'] = coords.x + ',' + coords.y + ',' + coords.x2 + ',' + coords.y2;
                obj['cropxunits'] = img.width();
                obj['cropyunits'] = img.height();
            });
        } else {
            setUrl(img, previousUrl);
        }
        jcrop_reference.destroy();
        img.attr('style',''); //Needed to fix all the junk JCrop added.
        btnCancel.hide();
        btnDone.hide();
        ratio.hide();
        btnCrop.show();
        preview.hide();
        accordion.accordion("enable");
    }

    var btnCrop = makeButton(img, "Crop", null, null, function () {
        var data = parse(img.attr('src'));
        delete data.obj["crop"];
        delete data.obj["cropxunits"];
        delete data.obj["cropyunits"];
        var uncroppedUrl = data.path + '?' + QueryString.stringify(data.obj);
        var image = new Image();
        image.onload = function () { startCrop(image.width,image.height, uncroppedUrl); };
        image.src = uncroppedUrl;
    }).appendTo(c);

    var ratio = $("<select></select>");
    var getRatio = function () {
        return ratio.val() == "current" ? img.width() / img.height() : (ratio.val() == 0 ? null : ratio.val())
    }
    var ratios = [[0, "Custom"], ["current", "Current"], [4 / 3, "4:3"], [16 / 9, "16:9 (Widescreen)"], [2 / 3, "2:3"]];
    for (var i = 0; i < ratios.length; i++)
        $('<option value="' + ratios[i][0].toString() + '">' + ratios[i][1] + '</option>').appendTo(ratio);
    ratio.appendTo(c).val(0).hide();
    ratio.change(function () {
        jcrop_reference.setOptions({ aspectRatio: getRatio() });
        jcrop_reference.focus();
    });

    var btnCancel = makeButton(img, "Cancel", null, null, function (obj) {
        stopCrop(false);
    }).appendTo(c).hide();
    var btnDone = makeButton(img, "Done", null, null, function (obj) {
        stopCrop(true);
    }).appendTo(c).hide();
    var preview = $("<div></div>").appendTo(c);
    var btnReset = makeButton(img, "Reset", null, function (obj) {
        stopCrop(false);
        delete obj["crop"];
        delete obj["cropxunits"];
        delete obj["cropyunits"];
    }).appendTo(c);
    


    //Update button label
    btnCrop.button("option", "label", getValue(img, "crop") ? "Modify crop" : "Crop");
    img.bind('query', function (e, obj) {
        btnCrop.button("option", "label", obj["crop"] ? "Modify crop" : "Crop");
    });

    accordion.append(c);
};

function studio(div) {
    div = $(div);
    var img = div.find("img.img");
    var a = div.find("div.controls");
    a.width("230px");
    a.css("height","600px");

    


    addRotateFlipPane(img, a);
    addCropPane(img, a, {});
    addAdjustPane(img, a, {});
    addRedEyePane(img, a, {});
    addCarvePane(img, a, {});
    addEffectsPane(img, a, {});
    a.accordion({  fillSpace:false});


}

$(function () {
    studio($("#studio"));
});