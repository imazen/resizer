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

using System::FlagsAttribute;

namespace ImageResizer{
    namespace Plugins {
        namespace FastScaling{

#define STATUS_CODE_NAME
#define STATUS_CODE_ENUM_NAME public enum class FastScalingResult

#include "status_code.h"

            namespace internal_use_only{
                public enum struct  Rotate{
                    RotateNone = 0,
                    Rotate90 = 1,
                    Rotate180 = 2,
                    Rotate270 = 3
                };

                public enum struct BitmapPixelFormat {
                    Bgr24 = 3,
                    Bgra32 = 4,
                    Gray8 = 1
                };


                public enum struct BitmapCompositingMode {
                    Replace_self = 0,
                    Blend_with_self = 1,
                    Blend_with_matte = 2
                };

                [Flags]
                public enum struct Workingspace: System::Int32 {
                    Floatspace_auto = -1,
                    Floatspace_as_is = 0,
                    Floatspace_srgb_to_linear = 1,
                    Floatspace_sigmoid = 2,
                    Floatspace_srgb_to_sigmoid = 2 | 1,

                    Floatspace_sigmoid_2 = 2 | 4,
                    Floatspace_srgb_to_sigmoid_2 = 1 | 2 | 4,

                    Floatspace_sigmoid_3 = 2 | 8,
                    Floatspace_srgb_to_sigmoid_3 = 1 | 2 | 8

                };


                public ref class ExecutionContext{
                public:
                    ExecutionContext (){
                        c = Context_create ();
                    }

                    float ByteToFloatspace (uint8_t value){
                        return Context_byte_to_floatspace (c,value);
                    }
                    uint8_t FloatspaceToByte (float value){
                        return Context_floatspace_to_byte (c, value);
                    }

                    void UseFloatspace (Workingspace space, float param_a, float param_b, float param_c){
                        Context_set_floatspace (this->c, (::WorkingFloatspace)(int)space, param_a, param_b, param_c);
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
