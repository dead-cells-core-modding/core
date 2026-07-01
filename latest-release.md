# Release Notes - v35.11.5 / 发布说明 - v35.11.5

## Feature

- Upgraded hashlink runtime to support SDL3 as the windowing and input backend, replacing SDL2 / 升级 hashlink 运行时以支持 SDL3 作为窗口和输入后端，替代 SDL2
- Added DirectX 12 rendering backend with shader compilation, command queues, resource barriers, pipeline state, debug layer, and PIX event support / 新增 DirectX 12 渲染后端，支持着色器编译、命令队列、资源屏障、管线状态、调试层和 PIX 事件
- Added plugin system for runtime loading of compiled HL modules with type compatibility verification / 新增插件系统，支持运行时加载已编译的 HL 模块并进行类型兼容性校验
- Added HGUID (GUID) type support for unique identifier handling / 新增 HGUID（GUID）类型，用于处理唯一标识符
- Added window dark mode, custom icon, and mouse capture APIs for both SDL and DirectX backends / 新增窗口深色模式、自定义图标和鼠标捕获 API，同时支持 SDL 和 DirectX 后端
- Added HLC build templates for Visual Studio up to version 2026 / 新增 HLC 构建模板，支持 Visual Studio 直至 2026 版本
- Added GC parallel marking with configurable mark threads for improved garbage collection throughput / 新增 GC 并行标记，支持配置标记线程数以提升垃圾回收吞吐量
- Added heap memory analysis tools with interactive dump exploration and improved data format / 新增堆内存分析工具，支持交互式转储浏览和改进的数据格式
- Added integrated sampling profiler with Chrome trace format export for performance analysis / 新增集成采样分析器，支持 Chrome trace 格式导出以进行性能分析
- Upgraded bundled libuv to version 1.52.0 with complete function wrapper coverage / 将内置 libuv 升级至 1.52.0 版本，并提供完整的函数封装覆盖
- Upgraded regex engine from PCRE to PCRE2 / 将正则引擎从 PCRE 升级至 PCRE2
- Added Emscripten build target for WebAssembly output / 新增 Emscripten 构建目标，用于输出 WebAssembly
- Added native Windows stack unwinding for improved crash diagnostics / 新增原生 Windows 栈回溯功能，改善崩溃诊断信息
- Added atomic operations and semaphore/condition variable synchronization APIs / 新增原子操作和信号量/条件变量同步 API
- Added interrupt-resistant standard I/O handling with automatic retry on EINTR / 新增中断恢复的标准 I/O 处理，在 EINTR 信号时自动重试

## Fix

- Fixed SDL window maximize and restoration behavior on Windows / 修复 SDL 窗口在 Windows 上的最大化和还原行为
- Fixed clipboard using ANSI encoding; now uses UTF-8 for proper character handling / 修复剪贴板使用 ANSI 编码，现使用 UTF-8 以正确处理字符
- Fixed struct loading and type checking in the plugin system / 修复插件系统中的结构体加载和类型检查
- Fixed potential buffer overflow during compact alignment operations / 修复紧凑对齐操作中的潜在缓冲区溢出
- Fixed stack overflow in same-type validation when plugins are loaded / 修复加载插件时同类验证导致的栈溢出
- Fixed unicode output encoding through stdout and stderr / 修复 stdout 和 stderr 的 Unicode 输出编码
- Fixed JIT signed integer division overflow when dividing INT_MIN by -1 / 修复 JIT 有符号整数除法在 INT_MIN 除以 -1 时的溢出
- Fixed various JIT code generation bugs: UI8/UI16 MOD, integer-to-GUID conversion, array access, F32 NaN detection, and struct-to-packed assignment / 修复多项 JIT 代码生成缺陷：UI8/UI16 MOD、整数到 GUID 转换、数组访问、F32 NaN 检测、结构体到紧凑类型的赋值
- Fixed GC segmentation fault during tracked allocation profiling / 修复追踪分配分析时的 GC 段错误
- Fixed Reflect.compare overflow handling for comparison values / 修复 Reflect.compare 的比较值溢出处理
- Fixed object map lookup when using enum values as keys / 修复使用枚举值作为键时的对象映射查询
- Fixed SSL socket receive operations in threaded contexts blocking garbage collection / 修复线程上下文中的 SSL 套接字接收操作阻塞垃圾回收
- Fixed build script permissions to be executable on Unix systems / 修复 Unix 系统上构建脚本的执行权限
- Fixed thread naming race conditions in the profiler / 修复分析器中线程命名的竞争条件
