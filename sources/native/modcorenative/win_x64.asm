
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
