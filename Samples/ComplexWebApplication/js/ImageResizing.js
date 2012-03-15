var ImageResizing = {};
var ir = ImageResizing;

ir.ResizeSettings = function (url) {
    var isString = (typeof url == 'string' || url instanceof String);
    if (!isString) {
        //If it's an object, shallow clone it.
        this.mergeWith(url, false);
        //normalize
        this.normalize();
        return this;
    }
    var c = url.indexOf('?');
    var sep = (c < 0 && url.indexOf(';') > -1) ? ';' : '&';
    if (c < 0 && url.indexOf('=') < 0) return; //No querystring for us....

    //Otherwise, process it
    this.mergeWith(ir.Utils.parseQuery(url.substr(Math.max(-1, c) + 1), sep), false);
    //normalize
    this.normalize();
};
var rs = ImageResizing.ResizeSettings;

rs.prototype.mergeWith = function (addition, overwrite) {
    for (var i in addition) if (i && addition.hasOwnProperty(i) && addition[i] !== undefined && addition[i] !== null) {
        if (overwrite || this[i] === undefined || this[i] === null) this[i] = addition[i];
    }
    return this;
};

rs.prototype.toQueryString = function (useSemicolons) {
    return (useSemicolons ? ';' : '?') + ir.Utils.stringifyQuery(this, useSemicolons ? ';' : '&');
};

//Deletes the specified keys from this query and returns the deleted pairs in a new object.
rs.prototype.remove = function (first) {
    var removed = {};
    if (!(first instanceof Array)) {
        removed[first] = this[first];
        delete this[first];

        for (var i = 0; i < arguments.length; i++) {
            removed[arguments[i]] = this[arguments[i]];
            delete this[arguments[i]];
        }
    } else {
        for (var i = 0; i < first.length; i++) {
            removed[first[i]] = this[first[i]];
            delete this[first[i]];
        }
    }
    return new ImageResizing.ResizeSettings(removed);
};


rs.prototype.normalize = function () {
    //lowercase everything
    for (var key in this) {
        if (!this.hasOwnProperty(key)) continue;
        var val = this[key];
        delete this[key];
        this[key.toLowerCase()] = val;
    }

    //Fix thumbnail/format
    this.undup('format', 'thumbnail');
    this.undup('sflip', 'srcflip');
    this.undup('width', 'w');
    this.undup('height', 'h');
    return this;
};

rs.prototype.undup = function(primary, secondary){
    if (this[primary] === null || this[primary] === "") delete this[primary];
    if (this[secondary] === null || this[secondary] === "") delete this[secondary];
    if (this[secondary] === undefined) return;
    
    if (this[primary] === undefined) this[primary] = this[secondary];
    delete this[secondary];
};


rs.prototype.toggle = function (key, defaultValue, deleteIf) {
    //Special handling for flip.x, flip.y, srcFlip.x, srcFlip.y, sFlip.x, sFlip.y
    if ((/flip\.[xy]$/i).test(key)) {
        var lastchar = key.charAt(key.length - 1);
        key = key.substring(0, key.length - 2);
        if (key.charAt(0) == 's') this.rotateFlipCrop(0, lastchar == 'x', lastchar == 'y'); //Must call before changing flip value. Have to undo x flip, then reapply x or y flip.
        var val = ir.Utils.toggleFlip(this[key], lastchar == 'x', lastchar == 'y'); 
        if (val == "00") delete this[key];
        else this[key] = val;
        return;
    }
    var val = ir.Utils.toBool(this[key]);
    val = !val;
    if (val == deleteIf) delete this[key];
    else this[key] = val;
};

rs.prototype.increment = function (key, offset, cycleLimit, defaultValue) {
    defaultValue = defaultValue ? defaultValue : 0;
    var val = (this[key] === undefined | this[key] === null) ? defaultValue : parseFloat(this[key]);
    val = (val + offset) % cycleLimit;
    if (key == "srotate") this.rotateFlipCrop(offset, false,false);
    this[key] = val;
    
};

rs.prototype.getBool = function (key) {
    return ir.Utils.toBool(this[key]);
};

rs.prototype.getCrop = function () {
    return new ir.CropRectangle().pullFrom(this);
};

rs.prototype.setCrop = function (cropObj) {
    new ir.CropRectangle(cropObj).pushTo(this);
};

rs.prototype.resetSourceRotateFlip = function () {
    var oldFlip = ir.Utils.parseFlip(this.sflip);
    this.rotateFlipCrop(this.srotate ? -parseFloat(this.srotate) : 0, oldFlip.x, oldFlip.y);
    delete this.srotate;
    delete this.sflip;
};

rs.prototype.rotateFlipCrop = function (rot, fx, fy) {
    var c = this.getCrop();
    //Only rotate if all items are present.
    if (!c.allPresent()) return;
    var oldFlip = ir.Utils.parseFlip(this.sflip);
    var oldAngle = parseFloat(this.srotate ? this.srotate : "0");
    var r = ir.Utils.flipRotateRect(c.x1, c.y1, c.x2, c.y2, c.xunits, c.yunits, oldAngle, oldAngle + rot,oldFlip.x, oldFlip.x ^ fx,oldFlip.y, oldFlip.y ^ fy);
    this.setCrop(r);
};


ir.CropRectangle = function (obj) {
    if (obj) {
        for (var i in obj) if (i && obj.hasOwnProperty(i) && obj[i] !== undefined && obj[i] !== null) {
            this[i] = obj[i];
        }
    }
};
ir.CropRectangle.prototype.pullFrom = function (query) {
    if (query.cropxunits) this.xunits = query.cropxunits;
    if (query.cropyunits) this.yunits = query.cropyunits;
    if (query.crop) {
        var vals = query.crop.split(',');
        var keys = ['x1', 'y1', 'x2', 'y2'];
        if (vals.length < keys.length) return o;
        for (var i = 0; i < keys.length; i++) {
            this[keys[i]] = parseFloat(vals[i]);
        }
    }
    return this;
};
ir.CropRectangle.prototype.pushTo = function (query) {
    if (this.xunits) query.cropxunits = this.xunits;
    if (this.yunits) query.cropyunits = this.yunits;
    query.crop = this.x1 + "," + this.y1 + "," + this.x2 + "," + this.y2;
};

ir.CropRectangle.prototype.allPresent = function () {
    return this.x1 != null && this.y1 != null && this.x2 != null && this.y2 != null && this.xunits != null && this.yunits != null;
};

ir.CropRectangle.prototype.stretchTo = function (width, height) {
    var n = new ir.CropRectangle( { xunits: width, yunits: height, x1: this.x1, y1: this.y1, x2: this.x2, y2: this.y2 });
    var xfactor = width / this.xunits;
    var yfactor = height / this.yunits;
    n.x1 *= xfactor;
    n.x2 *= xfactor;
    n.y1 *= yfactor;
    n.y2 *= yfactor;
    return n;
};
ir.CropRectangle.prototype.toCoordsArray = function () {
    return [this.x1, this.y1, this.x2, this.y2];
};

ir.Utils = {};

ir.Utils.parseUrl = function (url) {
    var c = url.indexOf('?');
    var sep = (c < 0 && url.indexOf(';') > -1) ? ';' : '&';
    if (c > -1) {
        var query = url.substr(c + 1);
        return { path: url.substr(0, c), query: query, obj: new ImageResizing.ResizeSettings(query) };
    }
    return { path: url, obj: new ImageResizing.ResizeSettings(), query: '' };
};

ir.Utils.changeServer = function (url, newServer) {
    var schemend = url.indexOf("://");
    if (schemend < 0) return ir.Utils.joinPath(newServer,url);
    var nextSlash = url.indexOf('/', schemend + 3);
    if (nextSlash < 0) return newServer; //no path??
    return ir.Utils.joinPath(newServer, url.substring(nextSlash,url.length));

}
ir.Utils.joinPaths = function (base, relative) {
    if (base.charAt(base.length - 1) == '/') base = base.substr(0,base.length -1);
    return base + ((relatve.charAt(0) != '/') ? '/' : '') + relative; 
}

ir.Utils.parseFlip = function (value) {
    if (value == null) return { x: 0, y: 0 };
    value = value.toString().toLowerCase();
    var split = { "both": [1, 1], "xy": [1, 1], "x": [1, 0], "y": [0, 1], "h": [1, 0], "v": [0, 1], "none": [0, 0] };
    return { x: split[value][0], y: split[value][1] };
};
ir.Utils.toggleFlip = function (originalValue, togglex, toggley) {
    var restore = { "11": "xy", "10": "x", "01": "y", "00": "none" };
    var flip = ir.Utils.parseFlip(originalValue);
    flip.x = flip.x ^ togglex;
    flip.y = flip.y ^ toggley;
    return restore[flip.x.toString() + flip.y.toString()];
};

ir.Utils.toBool = function (val, defaultValue) {
    if (val === null || val === undefined) return (defaultValue == true);
    if (val == "false") return false;
    if (val == "0") return false;
    if (val == "no") return false;
    return true;
}

//Rotates the specified point around (0,0), (oldWidth,0), or (0,oldHeight) depending on the angle. 
//Used to translate crop coordinates.
ir.Utils.flipRotatePoint = function (x, y, oldWidth, oldHeight, oldAngle, newAngle, oldFlipH, newFlipH, oldFlipV, newFlipV) {
    var normal = function (angle) {
        if ((angle % 90) != 0) throw "Specified angle is not a multiple of 90";
        return (((angle + 360 + 360) % 360) / 90);
    };
    oldAngle = normal(oldAngle);
    newAngle = normal(newAngle);
    
    //First, undo all flipping, then rotation
    if (oldFlipH ^ (oldAngle == 2)) x = oldWidth - x;
    if (oldFlipV ^ (oldAngle == 2))  y = oldHeight - y; 
    var t;
    if (oldAngle == 1) { t = x; x = y; y = oldWidth - t; t = oldHeight; oldHeight = oldWidth; oldWidth = t; }
    if (oldAngle == 3) { t = y; y = x; x = oldHeight - t; t = oldHeight; oldHeight = oldWidth; oldWidth = t; }

    //Reapply rotation, then flipping.
    if (newAngle == 3) { t = x; x = y; y = oldWidth - t; t = oldHeight; oldHeight = oldWidth; oldWidth = t; }
    if (newAngle == 1) { t = y; y = x; x = oldHeight - t; t = oldHeight; oldHeight = oldWidth; oldWidth = t; }

    if (newFlipH ^ (newAngle == 2)) x = oldWidth - x;
    if (newFlipV ^ (newAngle == 2)) y = oldHeight - y;

    return { x: x, y: y };
}
//Returns an array of 4 values, x1, y2, x2, y2, of a rectangle of the given aspect ratio centered in the given width/height
ir.Utils.getRectOfRatio = function (ratio, maxwidth, maxheight) {
    var w; var h;
    if (maxwidth / maxheight > ratio) {
        w = ratio * maxheight;
        h = w / ratio;
    } else {
        h = maxwidth / ratio;
        w = h * ratio;
    }
    var x = (maxwidth - w) / 2;
    var y = (maxheight - h) / 2;
    return [x, y, x + w, y + h];
};

ir.Utils.flipRotateRect = function (x1, y1, x2, y2, width, height, oldAngle, newAngle, oldFlipH, newFlipH, oldFlipV, newFlipV) {
    var p1 = ir.Utils.flipRotatePoint(x1, y1, width, height, oldAngle, newAngle, oldFlipH, newFlipH, oldFlipV, newFlipV);
    var p2 = ir.Utils.flipRotatePoint(x2, y2, width, height, oldAngle, newAngle, oldFlipH, newFlipH, oldFlipV, newFlipV);
    x1 = p1.x;
    x2 = p2.x;
    y1 = p1.y;
    y2 = p2.y;

    var t;

    if (y2 < y1) { t = y1; y1 = y2; y2 = t; }
    if (x2 < x1) { t = x1; x1 = x2; x2 = t; }

    //Flip units when needed
    if ((((((newAngle - oldAngle) + 360 + 360) % 360) / 90) % 2) == 1) {
        t = width; width = height; height = t;
    }

    var ret = { x1: x1, y1: y1, x2: x2, y2: y2, xunits: width, yunits: height };
    //console.log("Moving from " + oldAngle + " to " + newAngle + ", " + oldFlipH + " to " + newFlipH + " and " + oldFlipV + " to " + newFlipV);
    //console.log(ret); 
    return ret;
};

(function () {
    var QueryString = {};

    QueryString.unescape = function (str, decodeSpaces) {
        return decodeURIComponent(decodeSpaces ? str.replace(/\+/g, " ") : str);
    };

    QueryString.escape = function (str) {
        return encodeURIComponent(str);
    };


    var stack = [];
    /**
    * <p>Converts an arbitrary value to a Query String representation.</p>
    *
    * <p>Objects with cyclical references will trigger an exception.</p>
    *
    * @method stringify
    * @param obj {Variant} any arbitrary value to convert to query string
    * @param sep {String} (optional) Character that should join param k=v pairs together. Default: "&"
    * @param eq  {String} (optional) Character that should join keys to their values. Default: "="
    * @param name {String} (optional) Name of the current key, for handling children recursively.
    * @static
    */
    QueryString.stringify = function (obj, sep, eq, name) {
        sep = sep || "&";
        eq = eq || "=";
        if (isA(obj, null) || isA(obj, undefined) || typeof (obj) === 'function') {
            return name ? encodeURIComponent(name) + eq : '';
        }

        if (isBool(obj)) obj = obj ? "true" : "false";
        if (isNumber(obj) || isString(obj)) {
            return encodeURIComponent(name) + eq + encodeURIComponent(obj);
        }
        if (isA(obj, [])) {
            var s = [];
            name = name + '[]';
            for (var i = 0, l = obj.length; i < l; i++) {
                s.push(QueryString.stringify(obj[i], sep, eq, name));
            }
            return s.join(sep);
        }
        // now we know it's an object.

        // Check for cyclical references in nested objects
        for (var i = stack.length - 1; i >= 0; --i) if (stack[i] === obj) {
            throw new Error("QueryString.stringify. Cyclical reference");
        }

        stack.push(obj);

        var s = [];
        var begin = name ? name + '[' : '';
        var end = name ? ']' : '';
        for (var i in obj) if (_.has(obj,i)) {
            var n = begin + i + end;
            s.push(QueryString.stringify(obj[i], sep, eq, n));
        }

        stack.pop();

        s = s.join(sep);
        if (!s && name) return name + "=";
        return s;
    };

    QueryString.parseQuery = QueryString.parse = function (qs, sep, eq) {
        return _.reduce(_.map(qs.split(sep || "&"),pieceParser(eq || "=")),mergeParams);
    };

    // Parse a key=val string.
    // These can get pretty hairy
    // example flow:
    // parse(foo[bar][][bla]=baz)
    // return parse(foo[bar][][bla],"baz")
    // return parse(foo[bar][], {bla : "baz"})
    // return parse(foo[bar], [{bla:"baz"}])
    // return parse(foo, {bar:[{bla:"baz"}]})
    // return {foo:{bar:[{bla:"baz"}]}}
    var pieceParser = function (eq) {
        return function parsePiece(key, val) {
            if (arguments.length !== 2) {
                // key=val, called from the map/reduce
                key = key.split(eq);
                return parsePiece(
                    QueryString.unescape(key.shift(), true),
                    QueryString.unescape(key.join(eq), true)
                );
            }
            key = key.replace(/^\s+|\s+$/g, '');
            if (isString(val)) {
                val = val.replace(/^\s+|\s+$/g, '');
                // convert numerals to numbers
                if (!isNaN(val)) {
                    var numVal = +val;
                    if (val === numVal.toString(10)) val = numVal;
                }
            }
            var sliced = /(.*)\[([^\]]*)\]$/.exec(key);
            if (!sliced) {
                var ret = {};
                if (key) ret[key] = val;
                return ret;
            }
            // ["foo[][bar][][baz]", "foo[][bar][]", "baz"]
            var tail = sliced[2], head = sliced[1];

            // array: key[]=val
            if (!tail) return parsePiece(head, [val]);

            // obj: key[subkey]=val
            var ret = {};
            ret[tail] = val;
            return parsePiece(head, ret);
        };
    };

    // the reducer function that merges each query piece together into one set of params
    function mergeParams(params, addition) {
        return (
        // if it's uncontested, then just return the addition.
            (!params) ? addition
        // if the existing value is an array, then concat it.
            : (isA(params, [])) ? params.concat(addition)
        // if the existing value is not an array, and either are not objects, arrayify it.
            : (!isA(params, {}) || !isA(addition, {})) ? [params].concat(addition)
        // else merge them as objects, which is a little more complex
            : mergeObjects(params, addition)
        );
    };

    // Merge two *objects* together. If this is called, we've already ruled
    // out the simple cases, and need to do the for-in business.
    function mergeObjects(params, addition) {
        for (var i in addition) if (i && _.has(addition,i)) {
            params[i] = mergeParams(params[i], addition[i]);
        }
        return params;
    };

    // duck typing
    function isA(thing, canon) {
        return (
        // truthiness. you can feel it in your gut.
            (!thing === !canon)
        // typeof is usually "object"
            && typeof (thing) === typeof (canon)
        // check the constructor
            && Object.prototype.toString.call(thing) === Object.prototype.toString.call(canon)
        );
    };
    function isBool(thing) {
        return (
            typeof (thing) === "boolean"
            || isA(thing, new Boolean(thing))
        );
    };
    function isNumber(thing) {
        return (
            typeof (thing) === "number"
            || isA(thing, new Number(thing))
        ) && isFinite(thing);
    };
    function isString(thing) {
        return (
            typeof (thing) === "string"
            || isA(thing, new String(thing))
        );
    };

    ir.Utils.parseQuery = QueryString.parse;
    ir.Utils.stringifyQuery = QueryString.stringify;
})();