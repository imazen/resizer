#include "helpers.h"


int flow_vsnprintf(char * s, size_t n, const char * fmt, va_list v)
{
    if (n == 0) {
        return -1; // because MSFT _vsn_printf_s will crash if you pass it zero for buffer size.
    }
    int res;
#ifdef _WIN32
    // Could use "_vsnprintf_s(s, n, _TRUNCATE, fmt, v)" ?
    res = _vsnprintf_s(s, n, _TRUNCATE, fmt, v);
#else
    res = vsnprintf(s, n, fmt, v);
#endif
    if (n)
        s[n - 1] = 0;
    // Unix returns length output would require, Windows returns negative when truncated.
    return (res >= (int)n || res < 0) ? -1 : res;
}

int flow_snprintf(char * s, size_t n, const char * fmt, ...)
{
    int res;
    va_list v;
    va_start(v, fmt);
    res = flow_vsnprintf(s, n, fmt, v);
    va_end(v);
    return res;
}

bool has_err(flow_c * c, const char * file, int line)
{
    if (Context_has_error(c)) {
        Context_add_to_callstack(c, file, line);

        char buffer[2048];
        char buffer_trace[2048];
        Context_error_message(c, buffer, sizeof(buffer));
        Context_stacktrace(c, buffer_trace, sizeof(buffer_trace));
        fprintf(stderr, "%s\n%s", buffer, buffer_trace);
        fflush(stderr);
        
        return true;
    }
    return false;
}



bool create_path_from_relative(flow_c * c, const char * base_file, bool create_parent_dirs, char * filename,
    size_t max_filename_length, const char * format, ...)
{
    if (base_file == NULL) {
        FLOW_error(c, Invalid_argument);
        return false;
    }
    const char * this_file = base_file;
    char * last_slash = strrchr(this_file, '/');
    if (last_slash == NULL) {
        last_slash = strrchr(this_file, '\\');
    }
    if (last_slash == NULL) {
        FLOW_error(c, Invalid_internal_state);
        return false;
    }
    size_t length = last_slash - this_file;

    if (max_filename_length < length + 1) {
        FLOW_error(c, Invalid_internal_state);
        return false;
    }
    memcpy(&filename[0], this_file, length);
    va_list v;
    va_start(v, format);
    int res = flow_vsnprintf(&filename[length], max_filename_length - length, format, v);
    va_end(v);
    if (res == -1) {
        // Not enough space in filename
        FLOW_error(c, Invalid_internal_state);
        return false;
    }
    /// Create directories
    if (create_parent_dirs) {
        //flow_recursive_mkdir(&filename[0], false);
        //NOT implemented
        FLOW_error(c, Invalid_argument);
        return false;
    }
    return true;
}


bool flow_compare_file_contents(flow_c * c, const char * filename1, const char * filename2,
    char * difference_message_buffer, size_t buffer_size, bool * are_equal)
{
    FILE * fp1, *fp2;
#ifdef _MSC_VER
    if (fopen_s(&fp1, filename1, "r") != 0) {
#else
    if ((fp1 = fopen(filename1, "r") == NULL) {
#endif
        if (difference_message_buffer != NULL)
            flow_snprintf(difference_message_buffer, buffer_size, "Unable to open file A (%s)", filename1);
        *are_equal = false;
        return true;
    }
#ifdef _MSC_VER
    if (fopen_s(&fp2, filename2, "r") != 0) {
#else
    if ((fp2 = fopen(filename2, "r") == NULL) {
#endif
        if (difference_message_buffer != NULL)
            flow_snprintf(difference_message_buffer, buffer_size, "Unable to open file B (%s)", filename2);
        *are_equal = false;
        return true;
    }

    int byte_ix = -1;
    bool mismatch = false;
    int f1, f2;
    while (1) {

        do {
            f1 = getc(fp1);
            byte_ix++;
        } while (f1 == 13); // Ignore carriage returns

        do {
            f2 = getc(fp2);
        } while (f2 == 13); // Ignore carriage returns

        if ((f1 == EOF) ^ (f2 == EOF)) {
            // Only one of the files ended
            mismatch = true;

            if (difference_message_buffer != NULL)
                flow_snprintf(difference_message_buffer, buffer_size,
                    "Files are of different lengths: reached EOF at byte %d in %s first.", byte_ix,
                    f1 == EOF ? filename1 : filename2);
            break;
        }

        if (f1 == EOF) {
            break;
        }

        if (f1 != f2) {
            mismatch = true;

            if (difference_message_buffer != NULL)
                flow_snprintf(difference_message_buffer, buffer_size, "Files differ at byte %d: %d vs %d", byte_ix, f1,
                    f2);
            break;
        }
    }
    *are_equal = !mismatch;

    fclose(fp1);
    fclose(fp2);
    return true;
}