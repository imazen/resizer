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
    context->internal_malloc = malloc;
    context->internal_free = free;
    context->internal_calloc = calloc;
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
#ifdef DEBUG
    char buffer[1024];
    fprintf(stderr, "%s:%d Context_set_last_error the error registered was: %s\n", file, line, Context_error_message(context, buffer, sizeof(buffer)));
#endif
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

const char * Context_error_message(Context * context, char * buffer, size_t buffer_size)
{
    snprintf(buffer, buffer_size, "Error in file: %s line: %d status_code: %d reason: %s", context->error.file, context->error.line, context->error.reason, status_code_to_string(context->error.reason));
    return buffer;
}

void * Context_calloc(Context * context, size_t instance_count, size_t instance_size, const char * file, int line)
{
#ifdef DEBUG
    fprintf(stderr, "%s:%d calloc of %zu * %zu bytes\n", file, line, instance_count, instance_size);
#endif
    return context->internal_calloc(instance_count, instance_size);
}

void * Context_malloc(Context * context, size_t byte_count, const char * file, int line)
{
#ifdef DEBUG
    fprintf(stderr, "%s:%d malloc of %zu bytes\n", file, line, byte_count);
#endif
    return context->internal_malloc(byte_count);
}
