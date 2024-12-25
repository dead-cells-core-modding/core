
bits 32
section .text

extern _c_call_bridge_hl_to_cs
extern _c_call_bridge_hl_to_cs2
extern _c_call_bridge_hl_to_cs_fat

global _get_ebp
global _get_esp
global _asm_call_bridge_hl_to_cs
global _debug_break

_debug_break:
	int3
	ret

_die_loop:
	jmp _die_loop

_get_ebp:
	mov eax,ebp
	ret
_get_esp:
	mov eax,esp
	ret

_asm_call_bridge_hl_to_cs:
	mov eax, [esp] ;Get Return EIP
	cmp dword [eax+8], 1 ; Is Enabled?
	jz _acbltc_enabled
	;Call Orig
	mov ecx, [eax+4] ;Orig Func Ptr
	mov [esp], ecx
	ret

	_acbltc_enabled:

		cmp dword [eax], 1
		jz _acbltc_ret_xxm0
		cmp dword [eax], 2
		jz _acbltc_ret_fat

		lea eax, [rel _c_call_bridge_hl_to_cs]
		call eax
		jmp _acbltc_cleanup
		_acbltc_ret_xxm0:
			lea eax, [rel _c_call_bridge_hl_to_cs2]
			call eax
			jmp _acbltc_cleanup
		_acbltc_ret_fat:
			lea eax, [rel _c_call_bridge_hl_to_cs_fat]
			call eax
	_acbltc_cleanup:
		add esp, 4
		ret