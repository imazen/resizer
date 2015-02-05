// This is the main DLL file.

#include "Stdafx.h"
#include "ImageResizer.Plugins.FastScaling.h"
#include "colormatrix.h"
#include "bitmap_scaler.h"

#pragma managed


using namespace System;
using namespace System::Drawing;
using namespace System::Drawing::Imaging;
using namespace ImageResizer::Resizing;
using namespace System::Diagnostics;
using namespace System::Collections::Specialized;
using namespace System::Runtime::InteropServices;

namespace ImageResizer{
	namespace Plugins{
		namespace FastScaling {

			public ref class FastScalingPlugin : public ImageResizer::Resizing::BuilderExtension, IPlugin
			{
			protected:
                virtual RequestedAction InternalGraphicsDrawImage(ImageState^ s, Bitmap^ dest, Bitmap^ source, array<PointF>^ targetArea, RectangleF sourceArea, array<array<float, 1>^, 1>^ colorMatrix) override{
                    
                    NameValueCollection ^query = s->settingsAsCollection();

                    String^ fastScale = query->Get("fastscale");
					String^ sTrue = "true";
                    
                    
					if (fastScale != sTrue){
						return RequestedAction::None;
					}
                    
                    int withHalving = 0;
                    String^ turbo = query->Get("turbo");
                    if (turbo == sTrue)
                        withHalving = 1;

                    double blur = System::String::IsNullOrEmpty(query->Get("f.blur")) ? 1.0 :
                        System::Double::Parse(query->Get("f.blur"), System::Globalization::NumberFormatInfo::InvariantInfo);
                    
                    double window = System::String::IsNullOrEmpty(query->Get("f.window")) ? 0 :
                        System::Double::Parse(query->Get("f.window"), System::Globalization::NumberFormatInfo::InvariantInfo);

                    double sharpen = System::String::IsNullOrEmpty(query->Get("f.sharpen")) ? 0 :
                        System::Double::Parse(query->Get("f.sharpen"), System::Globalization::NumberFormatInfo::InvariantInfo);

                    bool linear_sharpen = System::String::IsNullOrEmpty(query->Get("f.ss")) ? false : true;

                    double neg_mult = System::String::IsNullOrEmpty(query->Get("f.nm")) ? 1 :
                        System::Double::Parse(query->Get("f.nm"), System::Globalization::NumberFormatInfo::InvariantInfo);

                    double integ_sharpen = System::String::IsNullOrEmpty(query->Get("f.is")) ? 0 :
                        System::Double::Parse(query->Get("f.is"), System::Globalization::NumberFormatInfo::InvariantInfo);

                    double min_scaled_weighted = System::String::IsNullOrEmpty(query->Get("min_scaled_weighted")) ? 0 :
                        System::Double::Parse(query->Get("min_scaled_weighted"), System::Globalization::NumberFormatInfo::InvariantInfo);

                    int kernel_radius = System::String::IsNullOrEmpty(query->Get("f.unsharp.radius")) ? 0 :
                        System::Double::Parse(query->Get("f.unsharp.radius"), System::Globalization::NumberFormatInfo::InvariantInfo);
                    double unsharp_sigma = System::String::IsNullOrEmpty(query->Get("f.unsharp.sigma")) ? 0 :
                        System::Double::Parse(query->Get("f.unsharp.sigma"), System::Globalization::NumberFormatInfo::InvariantInfo);



					RectangleF targetBox = ImageResizer::Util::PolygonMath::GetBoundingBox(targetArea);
                    if (targetBox.Location != targetArea[0] || targetArea[1].Y != targetArea[0].Y || targetArea[2].X != targetArea[0].X){
						return RequestedAction::None;
                    }


                    
                    InterpolationDetailsPtr details;
                    details = DetailsOriginal();
                    if (query->Get("f") == "0"){
                        details = DetailsDefault();
                    }
                    if (query->Get("f") == "1"){
                        details = DetailsGeneralCubic();
                    }
                    if (query->Get("f") == "2"){
                        details = DetailsCatmullRom();
                    }
                    if (query->Get("f") == "3"){
                        details = DetailsMitchell();
                    }
                    if (query->Get("f") == "4"){
                        details = DetailsRobidoux();
                    }
                    if (query->Get("f") == "5"){
                        details = DetailsRobidouxSharp();
                    }
                    if (query->Get("f") == "6"){
                        details = DetailsHermite();
                    }
                    if (query->Get("f") == "7"){
                        details = DetailsLanczos();
                    }
                    if (query->Get("f") == "8"){
                        details = DetailsLanczosSharp();
                    }
                    

                    details->allow_source_mutation = true;
                    details->use_halving = withHalving;
                    details->blur *= blur;
                    details->post_resize_sharpen_percent = (int)sharpen;
                    details->negative_multiplier = neg_mult;
                    details->kernel_radius = kernel_radius;
                    details->unsharp_sigma = unsharp_sigma;
                    details->use_interpolation_for_percent = min_scaled_weighted > 0 ? min_scaled_weighted :  0.3;
                    details->integrated_sharpen_percent = integ_sharpen;
                    details->linear_sharpen = linear_sharpen;

                    if (window != 0) details->window = window;

                    System::Diagnostics::Debug::WriteLine("filter={0}, window={1}, blur={2}", query->Get("f"), details->window, details->blur);
                    System::Diagnostics::Debug::WriteLine("y={0} + {1}*x^2 + {2} * x^3, y={3} + {4}*x + {5}*x^2 + {6} * x ^ 3",
                        details->p1, details->p2, details->p3, details->q1, details->q2, details->q3, details->q4);

                    for (double x = -3.0; x < 3; x += 0.25){
                        System::Diagnostics::Debug::WriteLine(x.ToString()->PadRight(5) + details->filter(details, x).ToString());
                    }
                        
                    BgraScaler ^scaler = gcnew BgraScaler();
                    scaler->ScaleBitmap(source, dest, Util::PolygonMath::ToRectangle(sourceArea), Util::PolygonMath::ToRectangle(targetBox), colorMatrix, details, s->Job->Profiler);
                    free(details);
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
                void ApplyMatrix(Bitmap ^img, array<array<float, 1>^, 1>^ colorMatrix)
                {
                    if (colorMatrix == nullptr) return;

                    BitmapBgraPtr bb;
                    WrappedBitmap ^wb = gcnew WrappedBitmap(img, bb);
                    float *cm[5];
                    for (int i = 0; i < 5; i++)
                    {
                        pin_ptr<float> row = &colorMatrix[i][0];
                        cm[i] = row;
                    }
                    InternalApplyMatrix(bb, cm);
                    delete wb;
                }
			};
			
		}
	}
}