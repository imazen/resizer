<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Studio.aspx.cs" Inherits="ComplexWebApplication.Studio" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <link type="text/css" href="css/reset.css" rel="Stylesheet" />
    <link type="text/css" href="css/ui-darkness/jquery-ui-1.8.16.custom.css" rel="Stylesheet" />	
	<link rel="stylesheet" href="css/jquery.Jcrop.css" type="text/css" /> 
    
    <style type="text/css">
           .selected {border:1px solid green; padding:3px;}
           .imagePicker img{ padding: 4px;
    </style>
</head>
<body bgcolor="black">
    <form id="form1" runat="server">
    <div class="imagePicker">
    <asp:Literal ID='images' runat="server" />
    </div>

    <div class="studio1" ></div>
    


    <script type="text/javascript" src="js/libs/jquery-1.7.1.min.js"></script>
    <script type="text/javascript" src="js/libs/underscore-min.js"></script>
    <script type="text/javascript" src="js/libs/jquery-ui-1.8.16.custom.min.js"></script>

    <script src="js/jquery.Jcrop.js" type="text/javascript"></script> 
    <script src="js/jquery.jcrop.preview.js" type="text/javascript"></script> 
    <script type="text/javascript" src="js/ImageResizing.js"></script>
    <script type="text/javascript" src="js/jquery.imagestudio.js"></script>
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
