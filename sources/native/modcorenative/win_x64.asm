
bits 64
section .text

global asm_prepare_exception_handle
global asm_return_from_exception

asm_prepare_exception_handle:
	lea rax, [rsp+8]
	mov r11, [rax-8] ;Data Table Pointer
	mov rsp, [r11] ; Register Store

	push rax ;RSP
	push rbx
	push rbp
	push rdi 
	push rsi
	push r12
	push r13
	push r14
	push r15
	mov [r11+16], rsp ;Save rsp

	mov rsp, rax
	mov rax, [r11+8] ;Target
	jmp rax

asm_return_from_exception:
	mov rsp, [rcx+16]
	pop r15
	pop r14
	pop r13
	pop r12
	pop rsi
	pop rdi
	pop rbp
	pop rbx
	pop rax

	mov rsp, rax
	ret