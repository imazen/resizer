#include "ImageResizer.Plugins.FastScaling.h"

#pragma once
#pragma managed


using namespace System;
using namespace System::Drawing;
using namespace System::Drawing::Imaging;
using namespace System::Diagnostics;
using namespace System::Collections::Specialized;
using namespace System::Runtime::InteropServices;
using namespace ImageResizer::Plugins::FastScaling::internal_use_only;

namespace ImageResizer{
    namespace Plugins {
        namespace FastScaling{

            [Serializable]
            public ref class FastScalingException : public Exception
            {
            private:
                static String^ CreateMessage (ExecutionContext^ context){
                    char buffer[2048];

                    FastScalingResult code = (FastScalingResult)Context_error_reason (context->GetContext ());

                    return code.ToString () + gcnew String ("\n") + gcnew String (Context_stacktrace (context->GetContext (), buffer, 2047));


                }
            public:

                property FastScalingResult ResultCode;
                FastScalingException (ExecutionContext^ context) : Exception (CreateMessage(context)){
                    ResultCode = (FastScalingResult)Context_error_reason (context->GetContext ());

                }
                FastScalingException () : Exception () {}
                FastScalingException (String^ message) : Exception (message) {}
                FastScalingException (String^ message, Exception^ inner) : Exception (message, inner) {}
            protected:
                FastScalingException (System::Runtime::Serialization::SerializationInfo^ info, System::Runtime::Serialization::StreamingContext context) : Exception (info, context) {}


            };



        }
    }
}
