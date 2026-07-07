
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
