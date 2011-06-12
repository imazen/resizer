<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<%@ Register Assembly="ImageStudio" TagPrefix="ImageStudio" Namespace="ImageStudio" %>
<%@ Register Assembly="AjaxControlToolkit" TagPrefix="AjaxToolKit" Namespace="AjaxControlToolkit" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script src="/Examples/jquery-1.4.2.min.js" type="text/javascript" language="javascript">
    </script>
</head>
<body>
    <script type="text/javascript">
        var $jq = jQuery.noConflict();
    </script>
    <form id="form1" runat="server">
    <div>
    
        <asp:ScriptManager ID="ScriptManager1" runat="server">
        </asp:ScriptManager>



        <asp:FileUpload runat="server" ID="fuTest" />
        <asp:Button runat="server" ID="btnSave" Text="Upload" OnClick="btnSave_OnClick" />



        <AjaxToolKit:ModalPopupExtender ID="mpeCropper" TargetControlID="btnIgnore"  PopupControlID="pnlCropper" CancelControlID="btnIgnore"  runat="server" />
        <asp:Button runat="server" ID="btnIgnore" style="display:none;" />

        <asp:Image runat="server" ID="imgFinal" Visible="false" />

        <script language="javascript" type="text/javascript">
            //chrome and other family of browsers have issues, once popup is loaded image is not yet
            //loaded, so when image does finaly load, image will apear in ackward place for the user
            //therefore you needto adjust the layout again to make sure that it repositions it self in the correct place.
            //AdjustLayout is called once image is loaded.
            function AdjustLayout() {
                $find('<%= mpeCropper.ClientID %>')._layout();
            }
        </script>

        <asp:Panel runat="server" ID="pnlCropper">

            <ImageStudio:Cropper runat="server" 
                                 ID="imgstdCropper" 
                                 JqueryExtension="$jq" 
                                 croppingAspectRatio="16 / 9" 
                                 croppingHeightMax="1000" 
                                 croppingHeightMin="300" 
                                 croppingWidthMax="1000" 
                                 croppingWidthMin="300" 
                                 OnErrorProcessingImage="imgstdCropper_ErrorProcessingImage"
                                 OnSuccesfullyProcessedImage="imgstdCropper_OnSuccesfullyProcessedImage"
                                 OnShow="imgstdCropper_OnShow"
                                 OnHide="imgstdCropper_OnHide"
                                 OnClientCropperImageLoad="AdjustLayout();"
                                 />
        </asp:Panel>



        <asp:Label runat="server" ID="lblError" />

    </div>
    </form>
</body>
</html>
