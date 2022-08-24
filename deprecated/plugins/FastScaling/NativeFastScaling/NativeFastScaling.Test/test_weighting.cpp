#include "catch.hpp"
#include "fastscaling_private.h"
#include "helpers.h"



TEST_CASE("Test output weights", "[fastscaling]")
{

    Context * c = Context_create();


    char filename[2048];
    if (!create_path_from_relative(c, __FILE__, false, filename, 2048, "/visuals/weights.txt")) {
        ERR(c);
    }

    FILE * output;
#ifdef _MSC_VER
    if (fopen_s(&output, filename, "w") != 0) {
#else
    if ((output = fopen(filename, "w") == NULL) {
#endif
        CAPTURE(filename);
        FLOW_error(c, Invalid_internal_state);
        REQUIRE_FALSE(true);
    }

    fprintf(output, "filter, from_width, to_width, weights");

    // Loop through every filter type, and for each filter type, try a variety of scaling ratios.
    // For each scaling ratio, export a row where columns contain the weights for the input pixels
    // filter, 2, from, 200, to, 300, weights, src, 0, (0.00001, 0.00200, 1.2200), 1, ...
    int32_t filter_id = 1;
    int32_t scalings[] = { /*downscale to 1px*/ 1, 1, 2, 1, 3, 1, 4, 1,  5, 1, 6, 1, 7, 1, 17, 1,
        /*upscale from 2px*/ 2, 3, 2, 4, 2, 5, 2, 17,
        /*other*/ 11,           7, 7, 3, 8, 4 };
    InterpolationFilter last_filter = Filter_NCubicSharp;
    uint32_t scaling_ix;
    for (filter_id = 1; filter_id <= (int32_t)last_filter; filter_id++) {
        for (scaling_ix = 0; scaling_ix < sizeof(scalings) / sizeof(int32_t); scaling_ix += 2) {
            int32_t from_width = scalings[scaling_ix];
            int32_t to_width = scalings[scaling_ix + 1];
            InterpolationFilter filter = (InterpolationFilter)filter_id;

            InterpolationDetails * details = InterpolationDetails_create_from(c, filter);

            ERR(c);

            LineContributions * lct = LineContributions_create(c, to_width, from_width, details);

            if (Context_has_error(c)) {
                CAPTURE(filter);
                ERR(c);
            }

            fprintf(output, "\nfilter_%02d,%3d,%3d, ", filter_id, from_width, to_width);

            for (uint32_t output_pixel = 0; output_pixel < lct->LineLength; output_pixel++) {
                PixelContributions * current = &lct->ContribRow[output_pixel];

                fprintf(output, "to %i: ", output_pixel);

                for (int32_t ix = current->Left; ix <= current->Right; ix++) {
                    float weight = current->Weights[ix - current->Left];
                    fprintf(output, (ix == current->Left) ? "(" : " ");
                    fprintf(output, "%.06f", weight);
                }
                fprintf(output, "), ");
            }
            LineContributions_destroy(c, lct);
            InterpolationDetails_destroy(c, details);
        }
    }

    fclose(output);

    char reference_filename[2048];
    if (!create_path_from_relative(c, __FILE__, false, reference_filename, 2048, "/visuals/reference_weights.txt")) {
        ERR(c);
    }

    char result_buffer[2048];
    memset(&result_buffer[0], 0, 2048);
    bool are_equal;
    REQUIRE(flow_compare_file_contents(c, filename, reference_filename, &result_buffer[0], 2048, &are_equal));
    ERR(c);
    CAPTURE(result_buffer);
    if (!are_equal) {
        char diff_command[4096];

        flow_snprintf(diff_command, 4096, "diff -w %s %s", filename, reference_filename);
        int ignore_result = system(diff_command); // just for the benefit of STDOUT
    }
    REQUIRE(are_equal);

    Context_destroy(c);
}