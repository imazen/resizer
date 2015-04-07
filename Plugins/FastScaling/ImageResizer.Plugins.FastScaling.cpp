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

namespace ImageResizer{
	namespace Plugins{
		namespace FastScaling {

			public ref class FastScalingPlugin : public ImageResizer::Resizing::BuilderExtension, IPlugin, IQuerystringPlugin
			{
                void SetupConvolutions(ExecutionContext^ c, NameValueCollection ^query, RenderOptions^ addTo){
                    int kernel_radius = (int)GetDouble(query, "f.unsharp.radius", 0);
                    double unsharp_sigma = GetDouble (query, "f.unsharp.sigma", 1.4);
                    double threshold = GetDouble (query, "f.unsharp.threshold", 0);

                    if (kernel_radius > 0){

                        addTo->KernelA_Struct = ConvolutionKernel_create_guassian_sharpen (c->GetContext (), unsharp_sigma, kernel_radius);

                    }

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

                virtual RequestedAction InternalGraphicsDrawImage(ImageState^ s, Bitmap^ dest, Bitmap^ source, array<PointF>^ targetArea, RectangleF sourceArea, array<array<float, 1>^, 1>^ colorMatrix) override{

                    NameValueCollection ^query = s->settingsAsCollection();

                    String^ fastScale = query->Get("fastscale");
					String^ sTrue = "true";


                    if (System::String::IsNullOrEmpty (query->Get ("f")) && (fastScale == nullptr || fastScale->ToLowerInvariant () != sTrue)){
						return RequestedAction::None;
					}

                    RenderOptions^ opts = gcnew RenderOptions();


                    opts->SamplingBlurFactor = (float)GetDouble (query, "f.blur", 1.0);

                    opts->SamplingWindowOverride = (float)GetDouble (query, "f.window", 0);

                    opts->Filter = (::InterpolationFilter)(uint32_t)(float)GetDouble (query, "f", 0);


                    opts->SharpeningPercentGoal = (float)(GetDouble (query, "f.sharpen", 0) / 200.0);

                    opts->SharpeningPercentGoal = fminf(fmaxf(0.0f, opts->SharpeningPercentGoal), 0.5f);

                    opts->InterpolateLastPercent = GetDouble (query, "f.interpolate_at_least", opts->InterpolateLastPercent);

                    opts->InterpolateLastPercent = opts->InterpolateLastPercent < 1 ? -1 : opts->InterpolateLastPercent;


                    Workingspace space = (Workingspace)(int)fmax (-1, fmin (200, GetDouble (query, "f.space", 1)));
                    float space_param_a = (float)GetDouble (query, "f.a", 0);
                    float space_param_b = (float)GetDouble (query, "f.b", 0);
                    float space_param_c = (float)GetDouble (query, "f.c", 0);

                    double preserve_which = fmax (-9.999, fmin (9.999, GetDouble (query, "f.preserve", 0)));
                    if (preserve_which != 0){
                        space = Workingspace::Floatspace_gamma;
                        double multiplier = Math::Pow (0.7 * (preserve_which / 10.0) + 1, 1.4);
                        space_param_a = 2.2 * multiplier;
                    }

                    //Without gamma correction is equal to setting f.preserve=-6.1515


                    //TODO: permit it to work with increments of 90 rotation
                    //Write polygon math method to determin the angle of the target area.

					RectangleF targetBox = ImageResizer::Util::PolygonMath::GetBoundingBox(targetArea);
                    if (targetBox.Location != targetArea[0] || targetArea[1].Y != targetArea[0].Y || targetArea[2].X != targetArea[0].X){
						return RequestedAction::None;
                    }

                    bool sourceFormatInvalid = (source->PixelFormat != PixelFormat::Format32bppArgb &&
                        source->PixelFormat != PixelFormat::Format24bppRgb &&
                        source->PixelFormat != PixelFormat::Format32bppRgb);

                    Bitmap^ copy = nullptr;
                    Graphics^ copyGraphics = nullptr;
                    try{

                        BitmapOptions^ a = gcnew BitmapOptions ();
                        a->AlphaMeaningful = true;
                        a->Crop = Util::PolygonMath::ToRectangle (sourceArea);

                        if (!sourceFormatInvalid){
                            a->AllowSpaceReuse = false;
                            a->Bitmap = source;
                        }
                        else{
                            copy = gcnew Bitmap (source->Width, source->Height, PixelFormat::Format32bppArgb);
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
                        b->AlphaMeaningful = true;
                        b->Crop = Util::PolygonMath::ToRectangle (targetBox);
                        b->Bitmap = dest;
                        b->Compositing = ImageResizer::Plugins::FastScaling::internal_use_only::BitmapCompositingMode::Blend_with_self;

                        opts->ColorMatrix = colorMatrix;

                        ExecutionContext^ context = gcnew ExecutionContext ();
                        try{
                            SetupConvolutions (context, query, opts);
                            ManagedRenderer^ renderer;
                            try{
                                renderer = gcnew ManagedRenderer (context, a, b, opts, s->Job->Profiler);
                            if (space != Workingspace::Floatspace_srgb_to_linear){
                                context->UseFloatspace (space, space_param_a, space_param_b, space_param_c);
                            }
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
                    return gcnew array < String^, 1 > {"f.sharpen", "f.unsharp.radius"};
                }

			};

		}
	}
}
