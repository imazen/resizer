<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="ImageStudio._Default" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title>StudioJS Demo</title>
    <link href="css/reset.css" rel="Stylesheet" type="text/css" />
    <link href="css/ui-darkness/jquery-ui-1.9.1.custom.css" type="text/css" rel="Stylesheet" />	
	<link href="css/jquery.Jcrop.css" rel="stylesheet" type="text/css" /> 
    <link href="css/jquery.imagestudio.css" rel="stylesheet" type="text/css" /> 

 
    <style type="text/css">
        .selected { border-color: white !important; }
        .imagePicker img{ border:2px solid #010101; padding: 5px;}
        .imagePicker {
           	height:150px; 
           	overflow:auto;
           	white-space:nowrap;
           	padding:5px;
           	margin-bottom:10px;
        }
        body{ background-color:Black;}
    </style>
</head>
<body>
    <form id="form1" runat="server">

    <div class="imagePicker">
    <asp:Literal ID='images' runat="server" />
    </div>

    <div class="studio1" ></div>

    <script src="js/libs/jquery-1.8.2.min.js" type="text/javascript" ></script>
    <script src="js/libs/underscore-min.js" type="text/javascript" ></script>
    <script src="js/libs/jquery-ui-1.9.1.custom.min.js" type="text/javascript"></script>

    <script src="js/jquery.Jcrop.js" type="text/javascript"></script> 
    <script src="js/jquery.jcrop.preview.js" type="text/javascript"></script> 
    <script src="js/ImageResizer.js" type="text/javascript"></script>
    <script src="js/jquery.overdraw.js" type="text/javascript"></script>
    <script src="js/jquery.imagestudio.js" type="text/javascript"></script>
    <script type="text/javascript">
        //<!--


        $(function () {
            var cl = {};
            cl.studio = $('div.studio1');


            $('.imagePicker img').click(function () {
                $('.imagePicker img').removeClass('selected');
                var e = $(this);
                e.addClass('selected');
                cl.thumb = e;
                cl.studio.ImageStudio({
                    url: e.attr('src'),
                    onchange: function (api) { e.attr('src', api.getStatus().url); }
                });
            });

            $($('.imagePicker img')[0]).click();
        });

        
        //-->
    </script>
    
    </form>
</body>
</html>