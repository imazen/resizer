// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
// This is the main DLL file.

#include "Stdafx.h"
#include "ImageResizer.Plugins.FastScaling.h"

#include "managed_bitmap_wrapper.h"
#include "rendering.h"

#pragma managed


using namespace System;
using namespace System::Drawing;
using namespace System::Drawing::Imaging;
using namespace ImageResizer::Resizing;
using namespace System::Diagnostics;
using namespace System::Collections::Specialized;
using namespace System::Runtime::InteropServices;
using namespace ImageResizer::Plugins::FastScaling::internal_use_only;
using namespace ImageResizer::ExtensionMethods;

namespace ImageResizer{
	namespace Plugins{
		namespace FastScaling {

			public ref class FastScalingPlugin : public ImageResizer::Resizing::BuilderExtension, IPlugin, IQuerystringPlugin
			{
                void SetupConvolutions(ExecutionContext^ c, NameValueCollection ^query, RenderOptions^ addTo){
                    //No unsharp mask support until it is higher quality
          /*          int kernel_radius = (int)GetDouble(query, "f.unsharp.radius", 0);
                    double unsharp_sigma = GetDouble (query, "f.unsharp.sigma", 1.4);
                    double threshold = GetDouble (query, "f.unsharp.threshold", 0);

                    if (kernel_radius > 0){

                        addTo->KernelA_Struct = ConvolutionKernel_create_guassian_sharpen (c->GetContext (), unsharp_sigma, kernel_radius);

                    }*/

                }
			protected:

                System::Double GetDouble (NameValueCollection^ query, String^ key, double defaultValue){
                    double d = 0;

                    if (System::String::IsNullOrEmpty (query->Get (key)) || !double::TryParse (query->Get (key), System::Globalization::NumberStyles::Any, System::Globalization::CultureInfo::InvariantCulture, d))
                    {
                        return defaultValue;
                    }
                    else{
                        return d;
                    }
                }

                RenderOptions^ ParseOptions (NameValueCollection^ query, bool downscaling, bool colorMatrixPresent){

                    String^ prefix = downscaling ? "down." : "up.";

                    RenderOptions^ opts = gcnew RenderOptions ();


                    opts->SamplingBlurFactor = (float)GetDouble (query, prefix + "blur", 1.0);

                    opts->SamplingWindowOverride = (float)GetDouble (query, prefix +  "window", 0);


                    int speed = (int)Math::Round (GetDouble (query, prefix + "speed", 0));


                    if (!downscaling){

                        speed = Math::Min (2, Math::Max (0, speed));

                        opts->HalvingAcceptablePixelLoss = 0; //Not relevant for upscaling
                        opts->InterpolateLastPercent = -1; //Not relevant for upscaling

                        //If we increase the speed, use a filter with a smaller lobe size.
                        opts->Filter = (uint32_t)(speed == 0 ? ::Filter_Ginseng : (speed == 1) ? ::Filter_Robidoux : ::Filter_RobidouxFast);


                    }
                    else{
                        float settings[][3] =
                        {
                            {::Filter_Robidoux, -1, 0},
                            {::Filter_Robidoux, 3.1f, 0},
                            {::Filter_Robidoux, 2.1f, 0.26f},
                            {::Filter_Robidoux, 2.1f, 0.51f},
                            {::Filter_Fastest, 2.1f, 0.51f},
                            {::Filter_Fastest, 1.0f, 0.99f },
                            {::Filter_Box, 1.0f, 16.0f}

                        };

                        int index_zero = 2;
                        int configuration_count = 7;


                        speed = Math::Min (configuration_count - 1 - index_zero, Math::Max(-1 * index_zero, speed));

                        float * selection = settings[speed + index_zero];

                        opts->Filter = (uint32_t)selection[0];
                        opts->InterpolateLastPercent = (double)selection[1];
                        opts->HalvingAcceptablePixelLoss = selection[2];

                    }


                    opts->Filter = (::InterpolationFilter) NameValueCollectionExtensions::Get<internal_use_only::InterpolationFilter> (query, prefix + "filter", (internal_use_only::InterpolationFilter)opts->Filter);

                    opts->ScalingColorspace = NameValueCollectionExtensions::Get<Workingspace> (query, prefix + "colorspace", Workingspace::Floatspace_as_is);
                    opts->ColorspaceParamA = (float)GetDouble (query, prefix + "colorspace.a", 0);
                    opts->ColorspaceParamB = (float)GetDouble (query, prefix + "colorspace.b", 0);
                    opts->ColorspaceParamC = (float)GetDouble (query, prefix + "colorspace.c", 0);

                    if (colorMatrixPresent){
                        opts->ScalingColorspace = Workingspace::Floatspace_as_is;
                    }

                    double preserve_which = GetDouble (query, prefix + "preserve", -1000.0);
                    if (preserve_which > -999){
                        preserve_which = fmax (-9.999, fmin (9.999, preserve_which));
                        opts->ScalingColorspace = Workingspace::Floatspace_gamma;
                        double multiplier = Math::Pow (0.7 * (preserve_which / 10.0) + 1, 1.4);
                        opts->ColorspaceParamA = (float)( 2.2 * multiplier);
                    }

                    //Without gamma correction is equal to setting f.preserve=-6.1515

                    return opts;
                }


                virtual RequestedAction InternalGraphicsDrawImage(ImageState^ s, Bitmap^ dest, Bitmap^ source, array<PointF>^ targetArea, RectangleF sourceArea, array<array<float, 1>^, 1>^ colorMatrix) override{

                    NameValueCollection ^query = s->settingsAsCollection();

                    String^ fastScale = query->Get("fastscale");
					String^ sTrue = "true";

                    if (!System::String::IsNullOrEmpty (query->Get ("f"))){
                        throw gcnew Exception ("&f is deprecated. Used &down.filter instead.");
                    }

                    if (System::String::IsNullOrEmpty (query->Get ("f.sharpen")) && (fastScale == nullptr || fastScale->ToLowerInvariant () != sTrue)){
						return RequestedAction::None;
					}

                    //TODO: permit it to work with increments of 90 rotation
                    //Write polygon math method to determine the angle of the target area.
                    RectangleF targetBox = ImageResizer::Util::PolygonMath::GetBoundingBox (targetArea);
                    if (targetBox.Location != targetArea[0] || targetArea[1].Y != targetArea[0].Y || targetArea[2].X != targetArea[0].X){
                        return RequestedAction::None;
                    }


                    bool downscaling = (targetBox.Width < sourceArea.Width && targetBox.Height < sourceArea.Height);


                    RenderOptions^ opts = ParseOptions (query, downscaling, colorMatrix != nullptr);


                    opts->SharpeningPercentGoal = (float)(GetDouble (query, "f.sharpen", 0) / 200.0);


                    bool mayIgnoreAlpha = colorMatrix == nullptr && source->PixelFormat == PixelFormat::Format24bppRgb;


                    bool ignorealpha = ImageResizer::ExtensionMethods::NameValueCollectionExtensions::Get<bool> (query, "f.ignorealpha", mayIgnoreAlpha);

                    bool sourceFormatInvalid = (source->PixelFormat != PixelFormat::Format32bppArgb &&
                        source->PixelFormat != PixelFormat::Format24bppRgb &&
                        source->PixelFormat != PixelFormat::Format32bppRgb);

                    Bitmap^ copy = nullptr;
                    Graphics^ copyGraphics = nullptr;
                    try{

                        BitmapOptions^ a = gcnew BitmapOptions ();
                        a->AlphaMeaningful = !ignorealpha;
                        a->Crop = Util::PolygonMath::ToRectangle (sourceArea);

                        if (!sourceFormatInvalid){
                            a->AllowSpaceReuse = false;
                            a->Bitmap = source;
                        }
                        else{
                            copy = gcnew Bitmap (source->Width, source->Height, ignorealpha ? PixelFormat::Format24bppRgb : PixelFormat::Format32bppArgb);
                            source->SetResolution(96, 96);
                            copyGraphics = System::Drawing::Graphics::FromImage (copy);
                            copyGraphics->CompositingMode = Drawing2D::CompositingMode::SourceCopy;
                            copyGraphics->DrawImageUnscaled (source, 0, 0);
                            delete copyGraphics;
                            copyGraphics = nullptr;
                            a->Bitmap = copy;
                            a->AllowSpaceReuse = true;
                            a->Readonly = false;
                        }

                        BitmapOptions^ b = gcnew BitmapOptions ();
                        b->AllowSpaceReuse = false;
                        b->AlphaMeaningful = !ignorealpha;
                        b->Crop = Util::PolygonMath::ToRectangle (targetBox);
                        b->Bitmap = dest;
                        b->Compositing = ignorealpha ? internal_use_only::BitmapCompositingMode::Replace_self : internal_use_only::BitmapCompositingMode::Blend_with_self;

                        opts->ColorMatrix = colorMatrix;

                        ExecutionContext^ context = gcnew ExecutionContext ();
                        try{
                            SetupConvolutions (context, query, opts);
                            ManagedRenderer^ renderer;
                            try{
                                renderer = gcnew ManagedRenderer (context, a, b, opts, s->Job->Profiler);
                                renderer->Render ();
                            }
                            finally{
                                delete renderer;
                            }
                        }
                        finally{
                            delete context;
                        }
                    }
                    finally{
                        if (copyGraphics != nullptr) delete copyGraphics;
                        if (copy != nullptr) delete copy;
                    }
                    return RequestedAction::Cancel;

				}
			public:
				virtual ImageResizer::Plugins::IPlugin^ Install(ImageResizer::Configuration::Config^ c) override{
					c->Plugins->add_plugin(this);
					return this;
				}
				virtual bool Uninstall(ImageResizer::Configuration::Config^ c) override{
					c->Plugins->remove_plugin(this);
					return true;
				}

                virtual System::Collections::Generic::IEnumerable<System::String^>^ GetSupportedQuerystringKeys (){
                    return gcnew array < String^, 1 > {"f.sharpen"}; //Only list the keys that would activate image processing by themselves, in the absence of any other commands
                }

			};

		}
	}
}
