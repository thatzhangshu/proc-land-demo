# Procedural Landmass Generation E21 调试副本

来源：Downloads/Procedural-Landmass-Generation-master.zip 内的 Proc Gen E21。
原工程版本：Unity 5.5.0f3。目标验证版本：本机 Unity 2022.3.51f1，Built-in Render Pipeline。
原 ZIP 未修改。此目录是独立 Unity 工程；请直接打开此目录，不要把 Assets 复制进外层 URP 工程。

## 打开与运行

1. 双击本目录 Open-Project.cmd，或 Unity Hub → Add project from disk，选择本目录。
2. 允许 Unity 升级这个副本，等待首次资源导入和脚本编译完成。
3. 打开 Assets/Scenes/Scene.unity，也可使用 Tools → Landmass → Open Demo Scene。
4. 点击 Play。原控制器使用 WASD 移动、鼠标转向、Shift 跑步、空格跳跃。原作者代码按 Esc 会暂停编辑器；再次点击 Play/暂停按钮恢复。
5. 自动检查：在非 Play 状态点击 Tools → Landmass → Run Smoke Test。测试会进入 Play，等待异步地形生成，检查可见地形网格、碰撞体、Main Camera、观察者是否位于地面之上，以及运行期间错误。成功输出 LANDMASS_SMOKE_PASS，失败输出 LANDMASS_SMOKE_FAIL，之后退出 Play。

自动检查仅验证启动场景，不覆盖持续行走、跨 Chunk 移动、全部 LOD 和长时间内存使用。

## 已修改

- ThreadedDataRequester：所有队列读取与写入统一加锁；每帧取快照后在锁外回调，修复原先 Count 随 Dequeue 递减导致每帧漏处理部分结果的问题；工作线程设为后台线程；生成和回调异常回到主线程报告；捕获请求所属实例，避免静态实例变化导致请求串入下一次运行。
- TerrainGenerator：第一次生成前读取真实 viewer 坐标，避免非原点出生时先加载原点周围地形。
- Terrain.shader：目标改为 3.5，并声明 2darray 要求，与已有 Texture2DArray 采样保持一致。Shader 编译和渲染仍需 Unity 内验证。
- 增加打开场景及运行检查菜单，增加本地编译验证脚本。

## 验证状态（2026-11-04）

- 已使用本机 Unity 2022.3.51f1 的引用程序集和 Roslyn 检查 C# 编译。
- 编辑器启动尝试在资源导入前失败：许可证客户端 IPC 连接等待 60 秒超时，Unity 返回 199。日志：compile.log。
- 这次失败发生在当前自动化执行环境；不能据此认定桌面 Unity Hub 的许可证已失效。
- 尚未完成 Unity 工程升级、资源导入、Shader 编译、Play Mode 检查、可视效果及角色行走验证。ProjectVersion.txt 保留原始版本，等待 Unity 实际升级后更新。

在 PowerShell 执行 `./Verify-Compilation.ps1` 可重复 C# 检查；它分别检查 Player 编译和带 UNITY_EDITOR 的编译，产物位于系统临时目录。C# 检查不能替代 Unity 中的资源与运行验证。

若 Hub 能正常打开本目录，使用上述 Smoke Test 完成运行验证。如果桌面启动同样出现许可证错误，先在 Unity Hub 中检查账号和许可证状态，再重试。
