// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once



#include "..\..\Plugins\FastScaling\weighting.h"
#include "..\..\Plugins\FastScaling\bitmap_compositing.h"



#pragma managed

using namespace System;
using namespace System::Drawing;
using namespace System::Collections::Specialized;
using namespace System::IO;
using namespace Xunit;
using namespace ImageResizer;
using namespace ImageResizer::Configuration;
using namespace ImageResizer::Plugins::FastScaling;


namespace ImageResizerPluginsFastScalingTestsCpp {

    static Bitmap ^BuildFast(Bitmap ^source, String ^i)
    {
        Config ^c = gcnew Config();

        FastScalingPlugin ^fs = gcnew FastScalingPlugin();
        fs->Install(c);

        Stream ^dest = gcnew MemoryStream();
        ImageJob^ j = gcnew ImageJob();
        j->InstructionsAsString = i;
        j->Source = source;
        j->Dest = Bitmap::typeid;

        c->Build(j);
        return (Bitmap^)j->Result;
    }
}