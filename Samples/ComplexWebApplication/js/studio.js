var edit = function (img, callback) {
    //Find the URL of the original image minus the querystring.
    var path = img.attr('src');
    var qs = "";
    var splitAt = path.indexOf('?');
    if (splitAt > 0) {
        qs = path.substr(splitAt + 1);
        path = path.substr(0, splitAt);
    }
    var obj = QueryString.parseQuery(qs);
    callback(obj);
    img.attr('src', path + '?' + QueryString.stringify(obj));
};

var toggleFlip = function (originalValue, togglex, toggley) {
    if (originalValue != null) originalValue = originalValue.toString().toLowerCase();
    var split = { "both": [1, 1], "xy": [1, 1], "x": [1, 0], "y": [0, 1], "h": [1, 0], "v": [0, 1], "none": [0, 0] };
    var restore = { "11": "xy", "10": "x", "01": "y", "00": "none" };
    var x = (originalValue != null) ? split[originalValue][0] : 0;
    var y = (originalValue != null) ? split[originalValue][1] : 0;
    x = x ^ togglex;
    y = y ^ toggley;
    return restore[x.toString() + y.toString()];
}
//Makes a button that edits the image's querystring.
var makeButton = function (img, text, icon, editCallback) {
    return $('<button type="button"></button>').button({ label: text, icons: icon != null ? { primary: "ui-icon-" + icon} : {}
    }).click(function () {
        edit(img, function (obj) {
            editCallback(obj);
        });
    })
};
var addToggleButton = function (img, container, text, icon, querystringKey) {
    if (!window.uniqueId) window.uniqueId = (new Date()).getTime();
    window.uniqueId++;
    var chk = $('<input type="checkbox" id="' + window.uniqueId + '" />');
    chk.appendTo(container);
    $('<label for="' + window.uniqueId + '">' + text + '</label>').appendTo(container);
    chk.button({ icons: { primary: "ui-icon-" + icon} }).click(function () {
        edit(img, function (obj) {
            if (obj[querystringKey] == "false") obj[querystringKey] = false;
            obj[querystringKey] = !obj[querystringKey];
        });
    });
    //TODO: update checked status based on img event qsupdate
};

//Adds a pane for rotating and flipping the source image
var addRotateFlipPane = function (img, accordion) {
    accordion.append('<h3><a href="#">Rotate &amp; Flip</a></h3>');
    var c = $('<div></div>');
    makeButton(img, "Rotate left", "arrowreturnthick-1-w", function (obj) {
        obj["sRotate"] = obj["sRotate"] == null ? -90 : obj["sRotate"] - 90;
    }).appendTo(c);
    makeButton(img, "Rotate right", "arrowreturnthick-1-e", function (obj) {
        obj["sRotate"] = obj["sRotate"] == null ? 90 : obj["sRotate"] + 90;
    }).appendTo(c);
    makeButton(img, "Flip vertical (180)", "arrowthick-2-n-s", function (obj) {
        obj["sFlip"] = toggleFlip(obj["sFlip"], false, true);
    }).appendTo(c);
    makeButton(img, "Mirror", "arrowthick-2-e-w", function (obj) {
        obj["sFlip"] = toggleFlip(obj["sFlip"], true, false);
    }).appendTo(c);
    accordion.append(c);
};

var addAdjustPane = function (img, accordion, options) {
    accordion.append('<h3><a href="#">Adjust image</a></h3>');
    var c = $('<div></div>');

    addToggleButton(img, c, "Auto-fix", "image", "a.equalize");

    c.append("<h3>Contrast</h3>");
    $("<div></div>").slider({ min: -1000, max: 1000, value: 0,
        change: function (event, ui) {
            edit(img, function (obj) {
                obj["s.contrast"] = ui.value / 1000;
            });
        }
    }).appendTo(c);
    c.append("<h3>Saturation</h3>");
    $("<div></div>").slider({ min: -1000, max: 1000, value: 0,
        change: function (event, ui) {
            edit(img, function (obj) {
                obj["s.saturation"] = ui.value / 1000;
            });
        }
    }).appendTo(c);
    c.append("<h3>Brightness</h3>");
    $("<div></div>").slider({ min: -1000, max: 1000, value: 0,
        change: function (event, ui) {
            edit(img, function (obj) {
                obj["s.brightness"] = ui.value / 1000;
            });
        }
    }).appendTo(c);
    makeButton(img, "Reset", "cancel", function (obj) {
        delete obj["s.contrast"];
        delete obj["s.saturation"];
        delete obj["s.brightness"];
    }).appendTo(c);



    accordion.append(c);
};

var addEffectsPane = function (img, accordion, options) {
    accordion.append('<h3><a href="#">Effects &amp; Filters</a></h3>');
    var c = $('<div></div>');

    addToggleButton(img, c, "Black & White", "image", "s.grayscale");
    addToggleButton(img, c, "Sepia", "image", "s.sepia");
    addToggleButton(img, c, "Negative", "image", "s.invert");

    c.append("<h3>Noise Removal</h3>");
    $("<div></div>").slider({ min: 0, max: 100, value: 0,
        change: function (event, ui) {
            edit(img, function (obj) {
                obj["a.removenoise"] = ui.value;
            });
        }
    }).appendTo(c);
    c.append("<h3>Oil painting</h3>");
    $("<div></div>").slider({ min: 0, max: 100, value: 0,
        change: function (event, ui) {
            edit(img, function (obj) {
                obj["a.oilpainting"] = ui.value;
            });
        }
    }).appendTo(c);
    c.append("<h3>Posterize</h3>");
    $("<div></div>").slider({ min: 0, max: 255, value: 0,
        change: function (event, ui) {
            edit(img, function (obj) {
                obj["a.posterize"] = ui.value;
            });
        }
    }).appendTo(c);
    c.append("<h3>Gaussian Blur</h3>");
    $("<div></div>").slider({ min: 0, max: 40, value: 0,
        change: function (event, ui) {
            edit(img, function (obj) {
                obj["a.blur"] = ui.value;
            });
        }
    }).appendTo(c);

    c.append("<h3>Sharpen (Unsharp mask)</h3>");
    $("<div></div>").slider({ min: 0, max: 15, value: 0,
        change: function (event, ui) {
            edit(img, function (obj) {
                obj["a.sharpen"] = ui.value;
            });
        }
    }).appendTo(c);

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

    accordion.bind('accordionchange', function (event, ui) {

        if (ui.oldContent.get(0) === c.get(0)) {
            //Shut down JCrop, restore filtered querystring items
            if (img.data('Jcrop') != null) img.data('Jcrop').destroy();
            //
        }
        if (ui.newContent.get(0) === c.get(0)) {
            //Start up JCrop, save old crop coordinates and strip them out

        }
    });

    //[Crop|Modify Crop]
    //[Done]
    //[Cancel]
    //[Undo Crop]

    accordion.append(c);
};

function studio(div) {
    div = $(div);
    var img = div.find("img.img");
    var a = div.find("div.controls");
    a.width("200px");
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