// This is the main DLL file.

#include "Stdafx.h"
#include "ImageResizer.Plugins.FastScaling.h"
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
				virtual RequestedAction InternalGraphicsDrawImage(ImageState^ s, Bitmap^ dest, Bitmap^ source, array<PointF>^ targetArea, RectangleF sourceArea, ImageAttributes^ imageAttributes) override{
                    
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

                    double blur = System::String::IsNullOrEmpty(query->Get("blur")) ? 1.0 :
                        System::Double::Parse(query->Get("blur"), System::Globalization::NumberFormatInfo::InvariantInfo);
                    
                    double window = System::String::IsNullOrEmpty(query->Get("window")) ? 0 :
                        System::Double::Parse(query->Get("window"), System::Globalization::NumberFormatInfo::InvariantInfo);

                    double sharpen = System::String::IsNullOrEmpty(query->Get("sharpen")) ? 0 :
                        System::Double::Parse(query->Get("sharpen"), System::Globalization::NumberFormatInfo::InvariantInfo);


					RectangleF targetBox = ImageResizer::Util::PolygonMath::GetBoundingBox(targetArea);
					if (targetBox.Location != targetArea[0] || targetBox.Width != (targetArea[1].X - targetArea[0].X)){
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
                    details->allow_source_mutation = true;
                    details->use_halving = withHalving;
                    details->blur *= blur;
                    details->post_resize_sharpen_percent = (int)sharpen;
                    if (window != 0) details->window = window;
                        
                    BgraScaler ^scaler = gcnew BgraScaler();
                    scaler->ScaleBitmap(source, dest, Util::PolygonMath::ToRectangle(sourceArea), Util::PolygonMath::ToRectangle(targetBox), details, s->Job->Profiler);
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
			};
			
		}
	}
}