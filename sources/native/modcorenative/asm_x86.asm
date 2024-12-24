

section .text

global _get_ebp
global _get_esp

_get_ebp:
	mov eax,ebp
	ret
_get_esp:
	mov eax,esp
	ret
