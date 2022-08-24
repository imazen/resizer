Aliases: /docs/plugins/imagebuilder /docs/extend/imagebuilder

# Order of command execution

RemoteReader, SqlReader, S3Reader, Gradient, and Image404 all finish their work before this process starts. 

1. &ignoreicc=true/false: (During LoadImage) controls if the ICC profile is applied to the image.
2  &page= and &frame=: Page selection (TIFF) or frame selection (Animated gif): 
2. &sFlip= and &sRotate : Flips/rotates source image
3. &bgcolor= 		: Sets the background of image. If &format doesn't support transparency, &bgcolor defaults to white.
4. &shadowColor and &shadowWidth draw a drop shadow behind the image
5. &paddingColor= is drawn over the shadow, around where the image. will be
6. &width, &height, &maxwidth, &maxheight, &crop, &rotate, &scale, &stretch determine the dimensions and placement of the image. 
7. If &carve=true is specified, the SeamCarvingPlugin seam carve the original image to fit the new aspect ratio. 
7. Simple filters are applied while the final copy is happening (they don't affect the bgcolor, padding, or drop shadow): &filter=grayscale, sepia, etc.
7. Advanced filters are applied afterwards, on the whole image. They affect bgcolor, padding, and drop shadow, but not the border or watermarks.
10. Draw border (&borderColor and &borderWidth)
11. Draw watermarks (&watermark=)
12. Flip final image (&flip=)
12. Save image (&quality, &colors, &format, &dither)

## Order of Method execution

This is the order in which methods are called. Use this to determine which method to override when creating a plugin.

	PrepareSourceBitmap(s);  // We select the page/frame and flip the source bitmap here
	PostPrepareSourceBitmap(s);
	Layout(s); //Layout everything (Calls the following methods through EndLayout)
		FlipExistingPoints(s);
		LayoutImage(s);
		PostLayoutImage(s);
		LayoutPadding(s);
		PostLayoutPadding(s);
		LayoutBorder(s);
		PostLayoutBorder(s);
		LayoutEffects(s);
		PostLayoutEffects(s);
		LayoutMargin(s);
		PostLayoutMargin(s);
		LayoutRotate(s);
		PostLayoutRotate(s);
		LayoutNormalize(s);
		PostLayoutNormalize(s);
		LayoutRound(s);
		PostLayoutRound(s);
		EndLayout(s);
	PrepareDestinationBitmap(s); //Create a bitmap and graphics object based on s.destSize
	Render(s); //Render using the graphics object
		RenderBackground(s);
		PostRenderBackground(s);
		RenderEffects(s);
		PostRenderEffects(s);
		RenderPadding(s);
		PostRenderPadding(s);
		CreateImageAttribues(s);
		PostCreateImageAttributes(s);
		RenderImage(s);
		PostRenderImage(s);
		RenderBorder(s);
		PostRenderBorder(s);
		PreRenderOverlays(s);
		RenderOverlays(s);
		PreFlushChanges(s);
		FlushChanges(s);
		PostFlushChanges(s);
	ProcessFinalBitmap(s); //Perform the final flipping of the bitmap.
	EndProcess(s);
