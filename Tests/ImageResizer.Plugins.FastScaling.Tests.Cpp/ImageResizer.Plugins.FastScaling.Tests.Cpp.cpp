// This is the main DLL file.

#include "stdafx.h"

#include "ImageResizer.Plugins.FastScaling.Tests.Cpp.h"

#pragma managed

using namespace System;
using namespace System::Drawing;
using namespace System::Collections::Specialized;
using namespace Xunit;
using namespace ImageResizer;
using namespace ImageResizer::Configuration;
using namespace ImageResizer::Plugins::FastScaling;

namespace ImageResizerPluginsFastScalingTestsCpp {

    public ref class TestsCpp
    {
    public:

        [Fact]
        void DummyTest()
        {
            String ^imgdir = gcnew String("..\\..\\..\\..\\Samples\\Images\\");
            Config ^c = gcnew Config();
            FastScalingPlugin ^fs = gcnew FastScalingPlugin();

            fs->Install(c);

            c->BuildImage(imgdir + "red-leaf.jpg", "out.jpg", "idth=400&fastscale=true&turbo=true");
        }
    };
}
