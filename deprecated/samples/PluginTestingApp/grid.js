grid.global = {};
grid.xaxis = [];
grid.yaxis = [];
grid.globalcontrols = [];

// Array Remove - By John Resig (MIT Licensed)
Array.remove = function(array, from, to) {
  var rest = array.slice((to || from) + 1 || array.length);
  array.length = from < 0 ? array.length + from : from;
  return array.push.apply(array, rest);
};

Array.removeValues = function(arr){
    var what, a= arguments, L= a.length, ax;
    while(L> 1 && arr.length){
        what= a[--L];
        while((ax= arr.indexOf(what))!= -1){
            arr.splice(ax, 1);
        }
    }
    return arr;
}
//And to take care of IE8 and below-

if(!Array.prototype.indexOf){
    Array.prototype.indexOf= function(what, i){
        i= i || 0;
        var L= this.length;
        while(i< L){
            if(this[i]=== what) return i;
            ++i;
        }
        return -1;
    }
}


var updateGrid = function () {
    $(".gridContent table").remove();
    var t = $("<table />");
    //Add a row for the column names
    //Add a row for each x-axis control
   
    //Get common settings from: grid.global
    var gsettings = {};
    for (var i = 0; i < grid.globalcontrols.length; i++)
    {
        var key = grid.commands[grid.globalcontrols[i]].key;
        if (grid.global[key] !== undefined){
            gsettings[key] = grid.global[key];
        }
    }

    //grid.xaxis, grid.yaxis

    var buildItems = function(array, combine){
        console.log({buildItems:array});
        if (array.length == 0) return [];
        var result = [];
        var firstitem = grid.commands[array[0]];
        for (var i = 0; i < firstitem.values.length; i++){
            if (!combine){
                var subItems = [{key:firstitem.key,value:firstitem.values[i]}];
                for( var j =0; j < array.length -1; j++) subItems.push({});
                result.push(subItems);
            }else{
                var combinations = buildItems(array.slice(1),combine);
                for (var i = 0; i < combinations.length; i++){
                    combinations[i].unshift({key:firstitem.key,value:firstitem.values[i]});
                    result.push(combinations[i]);
                }
            }
        }
        if (!combine){
            var remainder = buildItems(array.slice(1),combine);
            for (var i = 0; i < remainder.length; i++){
                remainder[i].unshift({});
                result.push(remainder[i]);
            }
        }
        return result;
    };

    var xCombine = false;
    var xItems = buildItems(grid.xaxis, xCombine);
    var yCombine = false;
    var yItems = buildItems(grid.yaxis, yCombine);

    console.log({grid:grid,xItems:xItems,yItems:yItems});
    //Row for names of Y fiels
    if (grid.yaxis.length > 0){
        var topRow = $("<tr />").append("<td />");
        for (var i =0; i < grid.yaxis.length; i++)
            $("<th class='yfield' />").text(grid.commands[grid.yaxis[i]].n).appendTo(topRow);
        for (var i = 0; i < Math.max(1,xItems.length); i++)
            topRow.append("<td />");
        topRow.appendTo(t);
    }

    //Rows for names and values of X fields
    for (var i = 0; i < grid.xaxis.length; i++){
        var valsRow = $("<tr />").append($("<th class='xfield' />").text(grid.commands[grid.xaxis[i]].n));
        for (var j =0; j < grid.yaxis.length; j++) $("<td />").appendTo(valsRow);
        for (var j =0; j < xItems.length; j++){
            var val = xItems[j][i].value;
            $("<td class='xval' />").text(val !== undefined ? val : "").appendTo(valsRow);
        }
        valsRow.appendTo(t);
    }

    //Rows with Y values and images
    for (var i = 0; i < Math.max(1,yItems.length); i++){
        var row = $("<tr />").append($("<td />"));
        //Combine Y settings
        var ysettings= {};
        //Add Y values
        for (var j =0; yItems.length > 0 && j < yItems[i].length; j++){
            var val = yItems[i][j].value;
            if (val !== undefined) {
              $("<td class='yval' />").text(val).appendTo(row);
              ysettings[yItems[i][j].key] = val;
            }
        }
        //Add images
        for (var j = 0; j < Math.max(1,xItems.length); j++){
            var xsettings = {};
            for (var k = 0; xItems.length > 0 && k < xItems[j].length; k++){
                var val = xItems[j][k].value;
                if (val !== undefined) xsettings[xItems[j][k].key] = val;
            }
            var combination = {};
            $.extend(combination, gsettings,xsettings,ysettings);
            var url = "";
            if (combination["_server"]) url = combination["_server"] + url;
            if (combination["_image"]) url += combination["_image"];
            delete combination["_server"];
            delete combination["_image"];
            url += "?" + QueryString.stringify(combination);

            $("<td />").append($("<img />").attr("src",url)).appendTo(row);

        }
        row.appendTo(t);
    }

    t.appendTo($(".gridContent"));
};

$(function () {

    var commandsList = $("<ul class='commands'/>").insertBefore($("div.trash"));
    for (var i = 0; i < grid.commands.length; i++) {
        var item = grid.commands[i];
        var btn = $("<div class='command'/>").text(item.n);
        btn.button();
        var li = $("<li class='command' />").append(btn);
        li.draggable({ helper: "clone" });
        li.attr("title", i);
        li.appendTo(commandsList);
    }

    var buildControl = function (ix) {
        if (isNaN(ix)) ix = parseInt(ix);
        var item = grid.commands[ix];

        grid.globalcontrols.push(ix);

        var d = $("<div class='control'/>");
        /*
        $("<button></button>").button({ text: false, icons: { primary: "ui-icon-close"} }).appendTo(d).click(function () {
            d.parent("li").remove(); //Remove parent li
            d.remove();
            delete grid.global[item.key];
            Array.removeValues(grid.globalcontrols, ix);
        });*/

        $("<span class='field'></span>").text(item.n + " (" + item.key + "):").appendTo(d);

        if (item.values.length == 2 && (item.values[0] === true || item.values[0] === false)
        && (item.values[1] === true || item.values[1] === false)){
            
            var check = $("<input type='checkbox' />");
            check.prop("checked", grid.global[item.key] !== undefined ? grid.global[item.key] : item.values[0]);
            check.change(function(){
                grid.global[item.key] = check.prop("checked");
                updateGrid();
                saveState();
            });
            check.appendTo(d);
        }else{

            var select = $("<select></select>").appendTo(d); //.attr("size", item.values.length)
            for (var i = 0; i < item.values.length; i++) {
                $("<option></option>").attr("value", item.values[i]).text(item.values[i]).appendTo(select);
            }
            if (grid.global[item.key] !== undefined)
                select.val(grid.global[item.key])
            else{
                select.val(item.values[0]);
                grid.global[item.key] = item.values[0];
            }
            select.change(function () {
                grid.global[item.key] = select.val();
                updateGrid();
                saveState();
            });
        }
        var li = $("<li></li>").attr('title',ix).append(d);
        li.draggable({
            revert: "invalid",
        });

        li.bind('moved', function () {
            Array.removeValues(grid.globalcontrols,ix);
        });
        return li;
    };

    $("div.controlsArea").droppable({
        activeClass: "drop-area ui-corner-all",
        hoverClass: "drop-area-hover",
        drop: function (event, ui) {
            buildControl(ui.draggable.attr("title")).appendTo($("ul.controls"));
            $(ui.draggable).trigger('moved');
            if (!$(ui.draggable).hasClass('command')) ui.draggable.remove();
            updateGrid();
            saveState();
        }

    });

    var buildAxisItem = function (ix, arrayParent, arrayName) {
    
        ix = parseInt(ix.toString());
        var item = grid.commands[ix];
        var thisIx = ix;
        var btn = $("<div class='command'/>").text(item.n);
        btn.button();
        var li = $("<li></li>").append(btn);
        li.attr("title", ix);
        li.draggable({
            revert: "invalid",
        });

        var array = arrayParent[arrayName];
        var last = array.length;
        array[last] = ix;

        li.bind('moved', function () {
            Array.removeValues(array,ix);
        });

        return li;
    };

    $(".axisDiv").droppable({
        activeClass: "drop-area ui-corner-all",
        hoverClass: "drop-area-hover",
        drop: function (event, ui) {
            var array = $(this).hasClass("x") ? "xaxis" : "yaxis";
            buildAxisItem(ui.draggable.attr("title"),grid,array).appendTo($(this).children("ul"));
            
            $(ui.draggable).trigger('moved');
            if (!$(ui.draggable).hasClass('command')) ui.draggable.remove();
            updateGrid();
            saveState();
        }

    });

    $("div.commands").droppable({
        activeClass: "drop-trash drop-area ui-corner-all",
        hoverClass: "drop-area-hover",
        drop:  function (event, ui) {
            $(ui.draggable).trigger('moved');
            if (!$(ui.draggable).hasClass('command')) ui.draggable.remove();
            updateGrid();
            saveState();
        }
    });
    $("div.commands, div.controlsArea, div.axisDiv").disableSelection();

    var restoreState = function(state){
        //Restore controls
        grid.global = state.g !== undefined ? state.g : {};
        grid.globalcontrols = [];
        grid.xaxis = [];
        grid.yaxis = [];
        $("ul.controls").empty();
        if (state.gc !== undefined) 
            for (var i =0; i < state.gc.length; i++)
                buildControl(state.gc[i]).appendTo($("ul.controls"));
        
        //Restore axis
        $("ul.axisitems").empty();
        if (state.x !== undefined)
            for(var i =0; i < state.x.length; i++)
                buildAxisItem(state.x[i],grid,"xaxis").appendTo($("ul.x"));
        if (state.y !== undefined)
            for(var i =0; i < state.y.length; i++)
                buildAxisItem(state.y[i],grid,"yaxis").appendTo($("ul.y"));
        
        updateGrid();

    };

    var saveState = function(){
        jQuery.bbq.pushState({gc: grid.globalcontrols,g:grid.global,x:grid.xaxis,y:grid.yaxis},2);
    };
    $(window).bind( 'hashchange', function(e) {
        restoreState(e.getState(true));
    });
    restoreState(jQuery.bbq.getState(true));
});