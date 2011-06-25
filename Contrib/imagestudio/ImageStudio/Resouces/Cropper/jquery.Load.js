var <ClientID>_editorID = '#<Image.ClientID>';

<JQExtension>(document).ready(function () {
    <JQExtension>(<ClientID>_editorID).Jcrop({
        onChange: <ClientID>_showCoords,
        onSelect: <ClientID>_showCoords,
        aspectRatio: <CroppingAspectRatio>,
        setSelect:   [ 0, 0, <CroppingWidthMin>, <CroppingHeightMin>],
        minSize: [ <CroppingWidthMin>, <CroppingHeightMin>]
    });
});

function <ClientID>_showCoords(c) {
    var xField = document.getElementById('<FieldX.ClientID>');
    var yField = document.getElementById('<FieldY.ClientID>');
    var widthField = document.getElementById('<FieldWidth.ClientID>');
    var heightField = document.getElementById('<FieldHeight.ClientID>');

    xField.value = c.x;
    yField.value = c.y;
    widthField.value = c.w;
    heightField.value = c.h;
}


<JQExtension>(document).ready(function () {
    <JQExtension>(<ClientID>_editorID).load(function ()
    {
        <OnClientCropperImageLoad>
    });
});