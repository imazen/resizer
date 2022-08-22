#pragma once

#include <sys/types.h>
#include <sys/stat.h>
#include <errno.h>
#include <stdio.h>
#include "string.h"
#include <stdbool.h>
#include <stdlib.h>
#include "fastscaling_private.h"
#ifdef _MSC_VER
#include "direct.h" //for _mkdir
#endif

#ifdef __cplusplus
extern "C" {
#endif

#ifdef _MSC_VER
#include "io.h"
#pragma warning(error : 4005)

#ifndef _UNISTD_H
#define _UNISTD_H 1

    /* This file intended to serve as a drop-in replacement for
    *  unistd.h on Windows
    *  Please add functionality as needed
    */

#include <stdlib.h>
#include <io.h>
#include <process.h> /* for getpid() and the exec..() family */
#include <direct.h> /* for _getcwd() and _chdir() */

#define srandom srand
#define random rand

    /* Values for the second argument to access.
    These may be OR'd together.  */
#define R_OK 4 /* Test for read permission.  */
#define W_OK 2 /* Test for write permission.  */
    //#define   X_OK    1       /* execute permission - unsupported in windows*/
#define F_OK 0 /* Test for existence.  */

#define access _access
#define dup2 _dup2
#define execve _execve
#define ftruncate _chsize
#define unlink _unlink
#define fileno _fileno
#define getcwd _getcwd
#define chdir _chdir
#define isatty _isatty
#define lseek _lseek
    /* read, write, and close are NOT being #defined here, because while there are file handle specific versions for
    * Windows, they probably don't work for sockets. You need to look at your app and consider whether to call e.g.
    * closesocket(). */

#define ssize_t int

#define STDIN_FILENO 0
#define STDOUT_FILENO 1
#define STDERR_FILENO 2

#define S_IRWXU = (400 | 200 | 100)
#endif
#else
#include "unistd.h"
#endif

#ifdef _MSC_VER
#define _CRT_SECURE_NO_WARNINGS
#endif

typedef Context flow_c;
int flow_vsnprintf(char * s, size_t n, const char * fmt, va_list v);
int flow_snprintf(char * s, size_t n, const char * fmt, ...);
bool has_err(flow_c * c, const char * file, int line);

bool create_path_from_relative(flow_c * c, const char * base_file, bool create_parent_dirs, char * filename,
    size_t max_filename_length, const char * format, ...);
bool flow_compare_file_contents(flow_c * c, const char * filename1, const char * filename2,
    char * difference_message_buffer, size_t buffer_size, bool * are_equal);

#define FLOW_error(context, status_code)                                                                               \
    Context_set_last_error(context, status_code, __FILE__, __LINE__)

#define FLOW_add_to_callstack(context) Context_add_to_callstack(context, __FILE__, __LINE__)

#define FLOW_error_return(context)                                                                                     \
    Context_add_to_callstack(context, __FILE__, __LINE__);                                              \
    return false

#define FLOW_error_return_null(context)                                                                                     \
    Context_add_to_callstack(context, __FILE__, __LINE__);                                              \
    return NULL

#define ERR(c) REQUIRE_FALSE(has_err(c, __FILE__, __LINE__))

#ifdef __cplusplus
}
#endif