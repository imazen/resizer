/*
 * Copyright (c) Imazen LLC.
 * No part of this project, including this file, may be copied, modified,
 * propagated, or distributed except as permitted in COPYRIGHT.txt.
 * Licensed under the Apache License, Version 2.0.
 */
#include "fastscaling.h"

bool test_contrib_windows(char *msg);

bool function_bounded(InterpolationDetails* details, char *msg, double input_start_value, double stop_at_abs, double input_step, double result_low_threshold, double result_high_threshold);

bool function_bounded_bi(InterpolationDetails* details, char *msg, double input_start_value, double stop_at_abs, double input_step, double result_low_threshold, double result_high_threshold);

bool test_details(InterpolationDetails* details, char *msg, double expected_first_crossing, double expected_second_crossing, double expected_near0, double near0_threshold, double expected_end);

char * test_filter(InterpolationFilter filter, char *msg, double expected_first_crossing, double expected_second_crossing, double expected_near0, double near0_threshold, double expected_end);

bool test_weight_distrib(char *msg);

InterpolationDetails*  sample_filter(InterpolationFilter filter, double x_from, double x_to, double *buffer, int samples);