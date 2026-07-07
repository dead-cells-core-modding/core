
#include <hl.h>
#include <math.h>

HL_EXPORT float hlc_fmodf(float a, float b)
{
	return fmodf(a, b);
}

HL_EXPORT double hlc_fmod(double a, double b)
{
	return fmod(a, b);
}

HL_EXPORT void hl_null_access_op( int op_idx )
{
	hl_error("Null access at OpCode #%d", op_idx);
	HL_UNREACHABLE;
}
