
bits 64
section .text

global get_ebp
global get_esp
global asm_call_bridge_hl_to_cs

extern c_call_bridge_hl_to_cs

get_ebp:
	mov rax,rbp
	ret
get_esp:
	mov rax,rsp
	ret

%macro acbhtc_copy_float 3
	test r10, %1
	jz acbhtc_fc_%2
	movq %2, %3
	acbhtc_fc_%2: 

%endmacro

asm_call_bridge_hl_to_cs:
	mov rax, [rsp] ;Get Return EIP(Table)

	;Call Orig
	mov r10, [rax+0x10] ;Orig Func Ptr
	jnz acbhtc_enabled
	mov [rbp], r10
	ret

	acbhtc_enabled:

		cmp dword [rax+0x4], 1 ;hasFloatArg
		jz acbhtc_fc_r9

		;Copy xxm args
		mov r10, [rax+0x08] ;argFloatMarks

		acbhtc_copy_float 1, rcx, xmm0
		acbhtc_copy_float 2, rdx, xmm1
		acbhtc_copy_float 4, r8, xmm2
		acbhtc_copy_float 8, r9, xmm3

		;Call c_call_bridge_hl_to_cs
		lea rax, [rel c_call_bridge_hl_to_cs]
		call rax

		push rax
		mov rax, [rsp+8]  ;Get Return EIP(Table)
		cmp dword [rax], 0
		jz acbhtc_return_rax

		pop rax
		movq xmm0, rax
		add rsp, 8
		ret
	acbhtc_return_rax:
		pop rax
		add rsp, 8 ;Remove ptr to table
		ret