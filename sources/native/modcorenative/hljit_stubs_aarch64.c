#include <hlmodule.h>

jit_ctx *hl_jit_alloc()
{
    return NULL;
}

void hl_jit_free( jit_ctx *ctx, h_bool can_reset )
{
    (void)ctx;
    (void)can_reset;
}

void hl_jit_reset( jit_ctx *ctx, hl_module *m )
{
    (void)ctx;
    (void)m;
}

void hl_jit_init( jit_ctx *ctx, hl_module *m )
{
    (void)ctx;
    (void)m;
}

int hl_jit_function( jit_ctx *ctx, hl_module *m, hl_function *f )
{
    (void)ctx;
    (void)m;
    (void)f;
    return 0;
}

void *hl_jit_code( jit_ctx *ctx, hl_module *m, int *codesize, hl_debug_infos **debug, hl_module *previous )
{
    (void)ctx;
    (void)m;
    (void)codesize;
    (void)debug;
    (void)previous;
    return NULL;
}

void hl_jit_patch_method( void *old_fun, void **new_fun_table )
{
    (void)old_fun;
    (void)new_fun_table;
}
