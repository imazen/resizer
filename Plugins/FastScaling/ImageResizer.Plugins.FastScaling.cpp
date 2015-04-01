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
                    double kernel_radius = System::String::IsNullOrEmpty(query->Get("f.unsharp.radius")) ? 0 :
                        System::Double::Parse(query->Get("f.unsharp.radius"), System::Globalization::NumberFormatInfo::InvariantInfo);
                    double unsharp_sigma = System::String::IsNullOrEmpty(query->Get("f.unsharp.sigma")) ? 1.4 :
                        System::Double::Parse(query->Get("f.unsharp.sigma"), System::Globalization::NumberFormatInfo::InvariantInfo);

                    double threshold = System::String::IsNullOrEmpty(query->Get("f.unsharp.threshold")) ? 0 :
                        System::Double::Parse(query->Get("f.unsharp.threshold"), System::Globalization::NumberFormatInfo::InvariantInfo);

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

                    opts->Filter = (InterpolationFilter)(uint32_t)(float)GetDouble (query, "f", 0);


                    opts->SharpeningPercentGoal = (float)GetDouble (query, "f.sharpen", 0) / 200.0;

                    opts->SharpeningPercentGoal = fminf(fmaxf(0.0f, opts->SharpeningPercentGoal), 0.5f);

                    opts->InterpolateLastPercent = GetDouble (query, "f.interpolate_at_least", opts->InterpolateLastPercent);

                    opts->InterpolateLastPercent = opts->InterpolateLastPercent < 1 ? -1 : opts->InterpolateLastPercent;

                    //TODO: permit it to work with increments of 90 rotation
                    //Write polygon math method to determin the angle of the target area.

					RectangleF targetBox = ImageResizer::Util::PolygonMath::GetBoundingBox(targetArea);
                    if (targetBox.Location != targetArea[0] || targetArea[1].Y != targetArea[0].Y || targetArea[2].X != targetArea[0].X){
						return RequestedAction::None;
                    }


                    BitmapOptions^ a = gcnew BitmapOptions();
                    a->AllowSpaceReuse = false;
                    a->AlphaMeaningful = true;
                    a->Crop = Util::PolygonMath::ToRectangle(sourceArea);
                    a->Bitmap = source;


                    BitmapOptions^ b = gcnew BitmapOptions();
                    b->AllowSpaceReuse = false;
                    b->AlphaMeaningful = true;
                    b->Crop = Util::PolygonMath::ToRectangle(targetBox);
                    b->Bitmap = dest;
                    b->Compositing = ImageResizer::Plugins::FastScaling::internal_use_only::BitmapCompositingMode::Blend_with_self;

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
                    }finally{
                        delete context;
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
