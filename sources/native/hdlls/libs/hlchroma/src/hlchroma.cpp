#define HL_NAME(n) chroma_##n
#include <Windows.h>
#include <hl.h>
#include <chroma/RzChromaSDKTypes.h>
#include <chroma/RzErrors.h>
#include <tchar.h>

#ifdef _WIN64
#	define CHROMA_LIBRARY_NAME _T("RzChromaSDK64.dll")
#else
#	define CHROMA_LIBRARY_NAME _T("RzChromaSDK.dll")
#endif

using namespace ChromaSDK;

typedef RZRESULT (*InitFunction)						();
typedef RZRESULT(*ReleaseFunction)						();
typedef RZRESULT (*ReleaseFunction)						();
typedef RZRESULT (*CreateEffectFunction)				(RZDEVICEID DeviceId, ChromaSDK::EFFECT_TYPE Effect, PRZPARAM pParam, RZEFFECTID *pEffectId);
typedef RZRESULT (*CreateKeyboardEffectFunction)		(ChromaSDK::Keyboard::EFFECT_TYPE Effect, PRZPARAM pParam, RZEFFECTID *pEffectId);
typedef RZRESULT (*CreateHeadsetEffectFunction)			(ChromaSDK::Headset::EFFECT_TYPE Effect, PRZPARAM pParam, RZEFFECTID *pEffectId);
typedef RZRESULT (*CreateMousePadEffectFunction)		(ChromaSDK::Mousepad::EFFECT_TYPE Effect, PRZPARAM pParam, RZEFFECTID *pEffectId);
typedef RZRESULT (*CreateMouseEffectFunction)			(ChromaSDK::Mouse::EFFECT_TYPE Effect, PRZPARAM pParam, RZEFFECTID *pEffectId);
typedef RZRESULT (*CreateKeypadEffectFunction)			(ChromaSDK::Keypad::EFFECT_TYPE Effect, PRZPARAM pParam, RZEFFECTID *pEffectId);
typedef RZRESULT (*CreateChromaLinkEffectFunction)		(ChromaSDK::ChromaLink::EFFECT_TYPE Effect, PRZPARAM pParam, RZEFFECTID *pEffectId);
typedef RZRESULT (*SetEffectFunction)					(RZEFFECTID EffectId);
typedef RZRESULT (*DeleteEffectFunction)				(RZEFFECTID EffectId);
typedef RZRESULT (*QueryDeviceFunction)					(RZDEVICEID DeviceId, ChromaSDK::DEVICE_INFO_TYPE &DeviceInfo);

CreateEffectFunction			createEffect			= NULL;
CreateKeyboardEffectFunction	createKeyboardEffect	= NULL;
CreateHeadsetEffectFunction		createHeadsetEffect		= NULL;
CreateMousePadEffectFunction	createMousePadEffect	= NULL;
CreateMouseEffectFunction		createMouseEffect		= NULL;
CreateKeypadEffectFunction		createKeypadEffect		= NULL;
CreateChromaLinkEffectFunction	createChromaLinkEffect	= NULL;
SetEffectFunction				setEffect				= NULL;
DeleteEffectFunction			deleteEffect			= NULL;


static HMODULE dllHandle = NULL;

HL_PRIM bool HL_NAME(init)(){
	if (dllHandle){
		return true;
	}
	else{
		dllHandle = LoadLibrary(CHROMA_LIBRARY_NAME);
		if (dllHandle){
			InitFunction init = (InitFunction)GetProcAddress(dllHandle, "Init");
			if (init){
				RZRESULT res = init();
				if (res == RZRESULT_SUCCESS){
					createEffect			= (CreateEffectFunction)			GetProcAddress(dllHandle, "CreateEffect");
					createKeyboardEffect	= (CreateKeyboardEffectFunction)	GetProcAddress(dllHandle, "CreateKeyboardEffect");
					createHeadsetEffect		= (CreateHeadsetEffectFunction)		GetProcAddress(dllHandle, "CreateHeadsetEffect");
					createMousePadEffect	= (CreateMousePadEffectFunction)	GetProcAddress(dllHandle, "CreateMousepadEffect");
					createMouseEffect		= (CreateMouseEffectFunction)		GetProcAddress(dllHandle, "CreateMouseEffect");
					createKeypadEffect		= (CreateKeypadEffectFunction)		GetProcAddress(dllHandle, "CreateKeypadEffect");
					createChromaLinkEffect	= (CreateChromaLinkEffectFunction)	GetProcAddress(dllHandle, "CreateChromaLinkEffect");
					setEffect				= (SetEffectFunction)				GetProcAddress(dllHandle, "SetEffect");
					deleteEffect			= (DeleteEffectFunction)			GetProcAddress(dllHandle, "DeleteEffect");
					return createEffect && createKeyboardEffect && createHeadsetEffect && createMousePadEffect && createMouseEffect && createKeypadEffect
						&& createChromaLinkEffect && setEffect && deleteEffect;
				}
			}
		}
	}
	return false;
}

HL_PRIM void HL_NAME(setMouseEffect)(void* params){
	if (dllHandle && createMouseEffect) {
		createMouseEffect(Mouse::CHROMA_CUSTOM2, params, NULL);
	}
}

HL_PRIM void HL_NAME(setKeyboardEffect)(void* params) {
	if (dllHandle && createKeyboardEffect) {
		createKeyboardEffect(Keyboard::CHROMA_CUSTOM, params, NULL);
	}
}

HL_PRIM void HL_NAME(setMousepadEffect)(void* params) {
	if (dllHandle && createMousePadEffect) {
		createMousePadEffect(Mousepad::CHROMA_CUSTOM, params, NULL);
	}
}

HL_PRIM void HL_NAME(setKeypadEffect)(void* params) {
	if (dllHandle && createKeypadEffect) {
		createKeypadEffect(Keypad::CHROMA_CUSTOM, params, NULL);
	}
}

HL_PRIM void HL_NAME(setHeadsetEffect)(void* params) {
	if (dllHandle && createHeadsetEffect) {
		createHeadsetEffect(Headset::CHROMA_CUSTOM, params, NULL);
	}
}

HL_PRIM void HL_NAME(setKeyboardKeysEffect)(void* params) {
	if (dllHandle && createKeyboardEffect) {
		createKeyboardEffect(Keyboard::CHROMA_CUSTOM_KEY, params, NULL);
	}
}

HL_PRIM void HL_NAME(setLinkedEffect)(void* params) {
	if (dllHandle && createChromaLinkEffect) {
		createChromaLinkEffect(ChromaLink::CHROMA_CUSTOM, params, NULL);
	}
}

HL_PRIM void HL_NAME(release)(){
	if (dllHandle) {
		ReleaseFunction release = (ReleaseFunction)GetProcAddress(dllHandle, "UnInit");
		if(release){
			RZRESULT res = release();
		}
		FreeLibrary(dllHandle);
		dllHandle = NULL;
	}
}
DEFINE_PRIM(_VOID, init, _NO_ARG)
DEFINE_PRIM(_VOID, setMouseEffect, _BYTES)
DEFINE_PRIM(_VOID, setKeyboardEffect, _BYTES)
DEFINE_PRIM(_VOID, setMousepadEffect, _BYTES)
DEFINE_PRIM(_VOID, setKeypadEffect, _BYTES)
DEFINE_PRIM(_VOID, setHeadsetEffect, _BYTES)
DEFINE_PRIM(_VOID, setKeyboardKeysEffect, _BYTES)
DEFINE_PRIM(_VOID, setLinkedEffect, _BYTES)
DEFINE_PRIM(_VOID, release, _NO_ARG)
