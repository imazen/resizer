// This is the main DLL file.

#include "stdafx.h"
#include "ImageResizer.Plugins.FastScaling.Tests.Cpp.h"

#include "..\..\Plugins\ImageResizer.Plugins.FastScaling\weighting.h"


bool test_contribs_unman()
{
    bool result = false;
    unsigned int from_w = 20;
    unsigned int to_w = 10;
    LineContribType *lct = 0;
    
    lct = ContributionsCalc(to_w, from_w, DetailsDefault());
    result = true;

    ContributionsFree(lct);
    return result;
}


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

        /*[Fact]
        void DummyTest()
        {
            String ^imgdir = gcnew String("..\\..\\..\\..\\Samples\\Images\\");
            Config ^c = gcnew Config();
            FastScalingPlugin ^fs = gcnew FastScalingPlugin();

            fs->Install(c);

            NameValueCollection ^s = gcnew NameValueCollection();
            s->Add("width", "400");
            s->Add("fastscale", "true");

            //ImageJob ^ij = gcnew ImageJob();
            //ImageJob ^ij = gcnew ImageJob(imgdir + "red-leaf.jpg", "out.jpg", s);
            //c->CurrentImageBuilder->Build(ij);

            c->BuildImage(imgdir + "red-leaf.jpg", "out.jpg", "width=400&fastscale=true&turbo=true");
        }*/

        [Fact]
        void ContributionsCalcTest()
        {
            Assert::True(test_contribs_unman());
        }
    };
}