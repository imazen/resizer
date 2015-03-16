#include "fastscaling_private.h"
#include "ir_alloc.h"
#include <stdlib.h>

static LookupTables * table = NULL;


void free_lookup_tables() {
    LookupTables * temp =  table;
    table = NULL;
    free(temp);
}

LookupTables * get_lookup_tables() {
    if (table == NULL){
        LookupTables * temp = (LookupTables*)ir_malloc(sizeof(LookupTables));
        if (temp == NULL) return NULL;
        // Gamma correction
        // http://www.4p8.com/eric.brasseur/gamma.html#formulas

        // Store gamma adjusted in 256-511, linear in 0-255

        float *lin = temp->linear;
        float *to_lin = temp->srgb_to_linear;

        for (uint32_t n = 0; n < 256; n++)
        {
            float s = ((float)n) / 255.0f;
            lin[n] = s;
            to_lin[n] = srgb_to_linear(s);
        }

        if (table == NULL){
            //A race condition could cause a 3KB, one-time memory leak between these two lines.
            //we're OK with that.
            table = temp;
        }
        else{
            ir_free(temp);
        }
    }
    return table;
}
