
bits 64
section .text

global _get_ebp
global _get_esp

_get_ebp:
	mov rax,rbp
	ret
_get_esp:
	mov rax,rsp
	ret
