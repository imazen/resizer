#include "fastscaling_private.h"
#include "ir_alloc.h"
#include <stdio.h>

#ifdef _MSC_VER
#pragma unmanaged
#pragma warning(disable : 4996)
#define snprintf _snprintf
#endif

void Context_initialize(Context * context) 
{
    context->error.file = NULL;
    context->error.line = -1;
    context->error.reason = No_Error;
    context->malloc = malloc;
    context->free = free;
    context->calloc = calloc;
}

int Context_error_reason(Context * context) 
{
    return context->error.reason;
}

void Context_set_last_error(Context * context, StatusCode code, const char * file, int line)
{
    context->error.reason = code;
    context->error.file = file;
    context->error.line = line;
}

bool Context_has_error(Context * context)
{
    return context->error.reason != No_Error;
}

const char * TheStatus = "The almight status has happened";
static const char * status_code_to_string(StatusCode code) 
{
    return TheStatus;
}

const char * Context_last_error_message(Context * context, char * buffer, size_t buffer_size)
{
    snprintf(buffer, buffer_size, "Error in file: %s line: %d reason: %s", context->error.file, context->error.line, status_code_to_string(context->error.reason));
    return buffer;
}
