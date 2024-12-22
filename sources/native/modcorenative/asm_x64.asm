

section .text

global _get_ebp

_get_ebp:
	mov rax,rbp
	ret
