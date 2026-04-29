# WCX 项目设计与进度说明

本文档是 WCX modernization 项目的中文入口说明，面向接手开发、代码审查和项目管理的开发者。恢复工作时请先读这里，再继续阅读 `docs/handoff.md`、最新 `docs/logs/`、`docs/roadmap.md` 和 `docs/development-guidelines.md`。

## 项目定位

WCX 是 WzComparerX 的现代化重建项目，目标是成为 WC（WzComparerR2）的现代继任者，而不是直接移植旧的 WinForms UI。

核心原则：

- 保留 WC 对 MapleStory WZ / IMG / PKG 资源格式的知识。
- 避免继承旧 UI、全局状态和插件耦合。
- 先完成可测试的 headless core 与 CLI，再逐步建设 Avalonia UI。
- 所有迁移来的真实格式行为都要能通过 `inspect` 或 `inspect --debug` 观察，并尽量用 fixture 或真实样本 smoke 记录锁定。

当前开发主线是：

```text
codex/wcx-modernization
```

除非用户明确要求，不要向 `origin` 推送。`origin` 的远端策略需要单独确认。

## 当前进度

截至 2026-04-29：

- Milestone 0：项目启动，已完成。
- Milestone 1：Headless Resource Browser，已完成。
- Milestone 2：First Real WC Migration，已完成。
- Milestone 3：Export And Inspect，已完成。
- Milestone 4：Basic Avalonia Browser，进行中，约 85% 以上。

项目整体已经具备：

- .NET 10 solution skeleton。
- Avalonia 桌面应用壳。
- CLI、Core、Domain、Rendering、WzLib 分层项目。
- WzLib 真实 WZ / IMG 解析切片。
- Core inspection / export / diagnostics 模型。
- CLI `list`、`header`、`headers`、`inspect`、`export` 工作流。
- Avalonia 基础资源浏览器。
- xUnit、CLI golden tests、Avalonia Headless UI tests、Skia 截图 smoke tests。

## 已完成的核心能力

### Headless 与 CLI

WCX 已经能在没有 UI 的情况下完成资源浏览和自动化检查：

- 读取 synthetic fixture。
- 枚举资源树。
- 检测 WZ `PKG1` / `PKG2` header。
- 递归扫描目录中的 WZ package header。
- 用 `inspect` 输出统一的 resource inspection tree。
- 用 `inspect --debug` 输出底层解析元数据和稳定 diagnostics。
- 用 `export` 导出 metadata、WC text-format IMG、Lua IMG、以及第一阶段 raw Canvas bytes。

当前支持的主要命令见 `docs/commands.md`。

### 真实 WZ / IMG 解析

Milestone 2 已迁移并锁定以下真实解析能力：

- PKG1 / PKG2 header detection。
- PKG1 directory enumeration。
- PKG1 recursive directory entries。
- PKG1 string name decoding 和 `0x02` string-reference names。
- WZ version、hash version、hash offset calculation。
- string key：`auto`、`none`、`noop`、`kms`、`gms`。
- IMG root object type detection。
- bounded `Property` traversal。
- scalar property values。
- Vector、Convex2D、UOL。
- Canvas / RawData / Video / Sound metadata。
- Lua IMG inspection。
- WC text-format IMG v1 / v2 inspection。

真实客户端 smoke 状态记录在 `docs/format-notes.md` 和 `docs/logs/2026-04-28-milestone-2-closeout.md`。

### Export 与 Diagnostics

Milestone 3 已经收口：

- `inspect`、`inspect --debug` 和 `export` 是正式的 headless automation surfaces。
- export 支持：
  - metadata JSON；
  - WC text-format IMG stream；
  - Lua IMG script；
  - direct-zlib raw Canvas bytes。
- Canvas export 使用 `--value <property-path>` 明确选择 IMG 内部 Canvas 值。
- diagnostics 具有稳定的 severity、source、code、path、CLI text formatting 和文档。
- PNG export、完整 Canvas pixel matrix、音视频 payload decode、完整 PKG2 directory parsing、XML dump 都是后续工作。

Canvas 相关设计见：

- `docs/canvas-decode-export-plan.md`
- `docs/canvas-export-selector-plan.md`

Diagnostics 规则见：

- `docs/diagnostics.md`

## 当前主线：Milestone 4 Basic Avalonia Browser

M4 目标是让 Avalonia UI 使用和 CLI 相同的 Core inspection / export 模型，而不是在 App 层重新实现解析逻辑。

当前 Avalonia UI 已具备：

- 路径输入和 `Load`。
- 单一 `Browse` 入口，包含 `Open Wz...` 和 `Open Folder...`。
- folder inspection：扫描目录中的 `.wz` package。
- package node open：在资源树中打开 package。
- IMG selector 输入。
- 选中 image node 后执行 `Inspect Image`。
- 双击 package / image node 触发对应 ViewModel 命令。
- Document metadata panel。
- Selection / Diagnostics panel。
- Canvas preview tab for the current narrow direct-zlib format `1` / `2` slice,
  including first-Canvas auto preview when selecting an IMG node and integer
  display scaling for small bitmaps plus proportional Auto shrink for large
  bitmaps. The Preview tab now exposes `Auto`, `1x`, `2x`, `4x`, `8x`, and
  `16x` display scale controls.
- Activity log panel。
- 在 IMG inspection 后返回当前 package directory。
- 对 GMS split-package 布局做 conservative linking，例如：
  - `Base/Base.wz` 下的 `Effect` 可链接到 `Effect/Effect.wz` 和 `Effect/Effect_000.wz`；
  - `UI/UI.wz` 下的 `_Canvas` 可链接到 `UI/_Canvas/_Canvas.wz` 和编号 shard。

M4 的测试体系已拆到 `WzComparerX.App.Tests`：

- ViewModel tests。
- Avalonia Headless window/control smoke tests。
- Skia-backed screenshot tests。
- TreeView selection / pointer click tests。
- invalid path、folder open、package open、image inspection visible state tests。

当前 M4 主要剩余工作：

- 真实本地 GMS UI smoke 记录。
- 评估是否把 split-package linking 从 `ResourceInspectionService` 拆出单独 Core helper。
- 扩展 Canvas preview/decode 格式覆盖，或先明确哪些格式进入 M4。
- 明确 M4 closeout 标准。
- 收口 UI 浏览器范围，避免提前进入 search、render、compare 等后续里程碑。

## 架构分层

### `WzComparerX.WzLib`

负责格式知识和底层解析：

- binary parsing；
- package header detection；
- WZ string decoding；
- IMG object / property inspection；
- Canvas / RawData / Sound / Video metadata；
- payload decode 的最小切片。

禁止依赖 UI、App settings、插件宿主或渲染后端。

### `WzComparerX.Core`

负责稳定业务模型和工作流编排：

- resource document / workspace；
- inspection model；
- export model；
- diagnostics model；
- folder/package inspection；
- split-package linking；
- CLI / UI 共用的服务边界。

Core 可以依赖 WzLib，但不能依赖 Avalonia。

### `WzComparerX.App`

负责 Avalonia UI：

- window；
- view models；
- file/folder picker；
- command binding；
- UI 状态和 activity log。

App 层应调用 Core service，不应重新实现 WZ / IMG parser。

### `WzComparerX.Domain`

预留给更高层 MapleStory 语义模型，例如 StringLinker、CharaSim、Avatar、Map 等。当前仍处于早期。

### `WzComparerX.Rendering`

预留给 renderer-independent bitmap / scene / frame projection。当前不要让它拥有 workspace 或直接加载 WZ。

## 测试策略

当前测试分布：

- `WzComparerX.WzLib.Tests`：底层格式和 reader 行为。
- `WzComparerX.Core.Tests`：Core service、CLI、inspection/export、golden output、split-package linking。
- `WzComparerX.App.Tests`：Avalonia ViewModel 和 Headless UI smoke tests。

重要规则：

- 迁移真实格式行为时要加 deterministic tests。
- 小 fixture 放在 `fixtures/synthetic/`。
- expected output 放在 `fixtures/expected/`。
- 真实客户端文件不能提交进仓库，只能做本地 smoke 并记录路径类别和结果摘要。
- M4 UI 行为测试放在 `WzComparerX.App.Tests`，不要塞进 Core tests。
- UI 可视回归优先使用 Avalonia Headless 和可选截图产物。

## 常用命令

恢复依赖：

```bash
dotnet restore WzComparerX.slnx
```

构建：

```bash
dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false
```

测试：

```bash
dotnet test WzComparerX.slnx --no-build -m:1
```

生成可选 UI 截图产物：

```bash
WCX_HEADLESS_SCREENSHOT_DIR=/private/tmp/wcx-headless-screens dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1
```

如果 build/test 在 Codex sandbox 中遇到 MSBuild named-pipe 权限问题，可以按工具提示使用 elevated permissions 重新运行。

## 文档地图

- `docs/handoff.md`：长线程恢复用的当前状态，信息最完整。
- `docs/roadmap.md`：里程碑和下一步队列。
- `docs/development-guidelines.md`：分层、测试、git、文档规则。
- `docs/architecture.md`：模块边界和依赖方向。
- `docs/migration-from-wc.md`：如何参考 WC 而不继承旧耦合。
- `docs/format-notes.md`：WZ / IMG / PKG 行为观察和真实样本 smoke 记录。
- `docs/commands.md`：CLI 命令和本地开发命令。
- `docs/diagnostics.md`：稳定 diagnostics 设计。
- `docs/canvas-decode-export-plan.md`：Canvas decode/export 最小切片。
- `docs/canvas-export-selector-plan.md`：Canvas value selector 设计。
- `docs/decision-log.md`：架构决策索引。
- `docs/adr/`：较长的架构决策记录。
- `docs/logs/`：每轮迭代日志。

## 恢复工作流程

每次恢复项目时建议按顺序执行：

1. `git status --short --branch`
2. 阅读本文件。
3. 阅读 `docs/handoff.md`。
4. 阅读 `docs/logs/` 中最新日志。
5. 阅读 `docs/roadmap.md`。
6. 阅读 `docs/development-guidelines.md`。
7. 运行 build/test。

不要在未理解当前分层和里程碑状态前直接扩功能。尤其是 UI 工作，应先确认是否可以复用 Core model/service。
