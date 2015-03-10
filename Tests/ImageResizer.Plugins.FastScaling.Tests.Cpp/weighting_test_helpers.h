#pragma once
#include "Stdafx.h"

bool test_contrib_windows(char *msg);

bool function_bounded(InterpolationDetailsPtr details, char *msg, double input_start_value, double stop_at_abs, double input_step, double result_low_threshold, double result_high_threshold);

bool function_bounded_bi(InterpolationDetailsPtr details, char *msg, double input_start_value, double stop_at_abs, double input_step, double result_low_threshold, double result_high_threshold);

bool test_details(InterpolationDetailsPtr details, char *msg, double expected_first_crossing, double expected_second_crossing, double expected_near0, double near0_threshold, double expected_end);

bool test_filter(InterpolationFilter filter, char *msg, double expected_first_crossing, double expected_second_crossing, double expected_near0, double near0_threshold, double expected_end);

bool test_weight_distrib(char *msg);

InterpolationDetailsPtr  sample_filter(InterpolationFilter filter, double x_from, double x_to, double *buffer, int samples);