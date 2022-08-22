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


#pragma managed
using System::FlagsAttribute;

namespace ImageResizer{
    namespace Plugins {
        namespace FastScaling{

            namespace internal_use_only{

#undef FASTSCALING_ENUMS_MANAGED
#undef STATUS_CODE_NAME
#undef FLOATSPACE_NAME

#define FASTSCALING_ENUMS_MANAGED
#define STATUS_CODE_NAME FastScalingResult
#define FLOATSPACE_NAME Workingspace

#include "fastscaling_enums.h"

#undef FASTSCALING_ENUMS_MANAGED
#undef STATUS_CODE_NAME
#undef FLOATSPACE_NAME

                public enum struct  Rotate{
                    RotateNone = 0,
                    Rotate90 = 1,
                    Rotate180 = 2,
                    Rotate270 = 3
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
