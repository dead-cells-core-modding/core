
bits 32
section .text

extern c_call_bridge_hl_to_cs
extern c_call_bridge_hl_to_cs2
extern c_call_bridge_hl_to_cs_fat

global get_ebp
global get_esp
global asm_call_bridge_hl_to_cs
global debug_break

debug_break:
	int3
	ret

die_loop:
	jmp die_loop

get_ebp:
	mov eax,ebp
	ret
get_esp:
	mov eax,esp
	ret

asm_call_bridge_hl_to_cs:
	mov eax, [esp] ;Get Return EIP
	cmp dword [eax+8], 1 ; Is Enabled?
	jz acbltc_enabled
	;Call Orig
	mov ecx, [eax+4] ;Orig Func Ptr
	mov [esp], ecx
	ret

	acbltc_enabled:

		cmp dword [eax], 1
		jz acbltc_ret_xxm0
		cmp dword [eax], 2
		jz acbltc_ret_fat

		lea eax, [rel c_call_bridge_hl_to_cs]
		call eax
		jmp acbltc_cleanup
		acbltc_ret_xxm0:
			lea eax, [rel c_call_bridge_hl_to_cs2]
			call eax
			jmp acbltc_cleanup
		acbltc_ret_fat:
			lea eax, [rel c_call_bridge_hl_to_cs_fat]
			call eax
	acbltc_cleanup:
		add esp, 4
		ret