#ifdef _MSC_VER
#ifndef DLLPROJECT_API 
#define DLLPROJECT_API __declspec(dllimport)
#endif
#else
#define DLLPROJECT_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

DLLPROJECT_API int dummy_function(void); 
#ifdef __cplusplus
}
#endif
