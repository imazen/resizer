/*
 * Copyright (c) Imazen LLC.
 * No part of this project, including this file, may be copied, modified,
 * propagated, or distributed except as permitted in COPYRIGHT.txt.
 * Licensed under the GNU Affero General Public License, Version 3.0.
 * Commercial licenses available at http://imageresizing.net/
 */
#pragma once
#include "Stdafx.h"
#include "fastscaling.h"

namespace ImageResizer{
    namespace Plugins {
        namespace FastScaling{

#define STATUS_CODE_NAME
#define STATUS_CODE_ENUM_NAME public enum class FastScalingResult

#include "status_code.h"

            namespace internal_use_only{
                public enum class  Rotate{
                    RotateNone = 0,
                    Rotate90 = 1,
                    Rotate180 = 2,
                    Rotate270 = 3
                };

                public enum class BitmapPixelFormat {
                    Bgr24 = 3,
                    Bgra32 = 4,
                    Gray8 = 1
                };


                public enum class BitmapCompositingMode {
                    Replace_self = 0,
                    Blend_with_self = 1,
                    Blend_with_matte = 2
                };


                public ref class ExecutionContext{
                public:
                    ExecutionContext (){
                        c = Context_create ();
                    }
                    ~ExecutionContext (){
                        if (c != nullptr){
                            Context_destroy (c);
                        }
                        c = nullptr;
                    }
                private:
                    Context* c;
                internal:

                    Context * GetContext (){
                        return c;
                    }
                };
            }
        }
    }
}

#include "error_translation.h"
