// This is the main DLL file.

#include "stdafx.h"
#include "ImageResizer.Plugins.FastScaling.Tests.Cpp.h"

#include "..\..\Plugins\ImageResizer.Plugins.FastScaling\weighting.h"


bool test_contrib_windows(char *msg)
{
    int bad = -1;
    LineContribType *lct = 0;

    // assumes included edge cases

    unsigned int from_w = 6;
    unsigned int to_w = 3;
    unsigned int corr36[3][2] = { { 0, 2 }, { 1, 4 }, { 3, 5 } };
    lct = ContributionsCalc(to_w, from_w, DetailsDefault());

    for (int i = 0; i < lct->LineLength; i++)
    if (lct->ContribRow[i].Left != corr36[i][0]) { bad = i; break; }
    else if (lct->ContribRow[i].Right != corr36[i][1]) { bad = i; break; }
    
    if (bad != -1)
    {
        sprintf(msg, "at 6->3 invalid value (%d; %d) at %d, expected (%d; %d)",
            lct->ContribRow[bad].Left,
            lct->ContribRow[bad].Right,
            bad, corr36[bad][0], corr36[bad][1]);
        ContributionsFree(lct);
        return false;
    }
    ContributionsFree(lct);

    from_w = 6;
    to_w = 4;
    unsigned int corr46[4][2] = { { 0, 1 }, { 1, 3 }, { 2, 4 }, { 4, 5 } };
    lct = ContributionsCalc(to_w, from_w, DetailsDefault());

    for (int i = 0; i < lct->LineLength; i++)
    if (lct->ContribRow[i].Left != corr46[i][0]) { bad = i; break; }
    else if (lct->ContribRow[i].Right != corr46[i][1]) { bad = i; break; }

    if (bad != -1)
    {
        sprintf(msg, "at 6->4 invalid value (%d; %d) at %d, expected (%d; %d)",
            lct->ContribRow[bad].Left,
            lct->ContribRow[bad].Right,
            bad, corr46[bad][0], corr46[bad][1]);
        ContributionsFree(lct);
        return false;
    }
    ContributionsFree(lct);

    return true;
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
            char msg[256];
            bool r = test_contrib_windows(msg);
            Assert::True(r, gcnew String(msg));
        }
    };
}