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

除非用户明确要求，不要向任何远端推送。当前本机远端布局是：

- `origin`：`Xseventh/WzComparerX`
- `upstream`：`Kagamia/WzComparerX`

## 当前进度

截至 2026-05-03：

- Milestone 0：项目启动，已完成。
- Milestone 1：Headless Resource Browser，已完成。
- Milestone 2：First Real WC Migration，已完成。
- Milestone 3：Export And Inspect，已完成。
- Milestone 4：Basic Avalonia Browser，已完成。
- 当前主线：Milestone 5 Parser Coverage And Resource Model Baseline。

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
- `docs/parser-coverage-matrix.md`

Resource identity 规则见：

- `docs/resource-identity.md`

## Milestone 4 Basic Avalonia Browser

M4 目标是让 Avalonia UI 使用和 CLI 相同的 Core inspection / export 模型，而不是在 App 层重新实现解析逻辑。

当前 Avalonia UI 已具备：

- 路径输入和 `Load`。
- 单一 `Browse` 入口，包含 `Open Package...` 和 `Open Folder...`。
- folder inspection：扫描目录中的 `.wz` / `.ms` / `.mn` package。
- package node open：在资源树中打开 package。
- IMG selector 输入。
- Resources tree 和 IMG Content tree 分离：选中 image node 会按 WC 的
  `TryExtract()` 体验自动提取完整单个 IMG 到 IMG Content tree，Resources
  tree 保持 package / directory 结构。
- `Load IMG` 只作为手动 selector / refresh 入口；手动 selector 会按
  WC-style package group 查找 numbered shard，资源树里选中 image node 时会
  自动加载 IMG Content。
- 双击 package / image node 触发对应 ViewModel 命令；package open 不再作为
  主工具栏按钮暴露。
- Document metadata panel。
- Selection / Diagnostics panel。
- Canvas preview tab for the current direct-zlib `ARGB4444` (`1`),
  `ARGB1555` (`257`), `RGB565` (`513`), `R16` (`769`), `ARGB8888` (`2`),
  `A8` (`2304`), `RGBA1010102` (`2562`), `DXT3` (`1026`), `DXT5`
  (`2050`), `DXT1` (`4097`), `BC7` (`4098`), and `RGBA32Float` (`4100`) slices,
  plus WC's `RGB565 scale=4` expansion path, following the selected IMG Content node.
  Selecting Canvas nodes previews that
  exact value; selecting `source` / `_inlink` / `_outlink` string nodes resolves
  the linked Canvas when the current workspace layout can be mapped. The Preview
  tab exposes `Auto`, `0.25x`, `0.5x`, `1x`, `2x`, `4x`, `8x`, and `16x`
  display scale controls. Preview defaults to `1x`; `Auto` fits the full
  current Preview viewport once and stores that fixed scale for subsequent
  images until the user changes scale again.
- Activity log panel。
- 对 GMS split-package 布局做 WC-style package group 合并，例如：
  - 打开 `Map1.wz` 时如果旁边有 `Map1.ini`，会按 `LastWzIndex` 合并
    `Map1_000.wz...` 的目录项；
  - 没有 `.ini` 时会 fallback 连续枚举 `Name_000.wz`、`Name_001.wz`；
  - merged shard 的 IMG node 仍保留原始 shard package target；手动
    `inspect` / `Load IMG` / Canvas export 也会从入口包 fallback 到同组
    numbered shard；
  - `Base/Base.wz` 下的 `Effect` 可链接到 `Effect/Effect.wz` 和 `Effect/Effect_000.wz`；
  - `UI/UI.wz` 下的 `_Canvas` 可链接到 `UI/_Canvas/_Canvas.wz` 和编号 shard。

M4 的测试体系已拆到 `WzComparerX.App.Tests`：

- ViewModel tests。
- Avalonia Headless window/control smoke tests。
- Skia-backed screenshot tests。
- TreeView selection / pointer click tests。
- invalid path、folder open、package open、image inspection visible state tests。

M4 closeout 标准已经满足：

- UI 继续只通过 Core inspection/export/canvas services 访问资源。
- Resources tree / IMG Content tree / Canvas Preview 三段主流程可用，并有
  Avalonia Headless 或 ViewModel 测试覆盖。
- 本地 GMS smoke 覆盖 package group merge、选中 IMG 自动填充 IMG Content、
  Canvas Preview、大目录和大 Canvas auto-scaling。
- 当前 direct-zlib Canvas preview 格式 `1` / `2` 足够作为 M4 closeout；更宽
  Canvas 格式矩阵、PNG export、MapRender 进入后续里程碑。
- `MainWindowViewModel` 和 Core package-group/linking 的进一步拆分在 M4
  closeout 后进行，不在收尾前做大重构。

2026-05-01 的本地 GMS UI smoke 已验证 `Map/Map/Map1/Map1.wz`：

- `Map1.ini` / `Map1_000.wz` package group merge。
- 选中 `100000000.img` 后 Resources tree 保持 package 结构，IMG Content tree
  自动填充完整 IMG。
- `miniMap/canvas` 的 1x1 placeholder 通过 `_outlink` 解析到真实 Canvas 并显示
  Preview。
- 大目录和大 Canvas auto-scale 路径可用。
- 截图为本地产物，不提交仓库；结果记录见
  `docs/logs/2026-05-01-m4-gms-ui-smoke.md`。

M4 完成后已经开始低风险架构整理：

- Canvas Preview 的节点识别、value selector 决策和 Core 调用被拆到 App
  workflow helper，`MainWindowViewModel` 只保留 UI 状态、请求防抖和 activity
  更新。
- IMG Content 的 selector 解析、重复 target 判断和 Core inspect 调用也被拆到
  App workflow helper，ViewModel 继续只负责可见状态和请求排序。
- Document / Selection / Diagnostics 面板的展示投影被拆到 ViewModel projection
  helper，方便后续复用同一套资源详情展示规则。
- Core split-package linking 的路径解析策略被拆到独立 helper，`ResourceInspectionService`
  继续负责 inspection tree 组合，不直接承载 workspace/package 路径推导细节。
- Core inspection node 现在带有 `ResourceInspectionIdentity`，用于明确 package
  source、image selector 和 IMG 内部 value path，给后续 Compare/Search/export
  提供不依赖 UI 字符串猜测的资源身份地基。
- IMG 内部 `source`、`_inlink`、`_outlink`、`link` 字符串和 UOL 节点现在会把
  normalized linked target 写进 Core identity，并在 `inspect --debug` 中暴露
  `linkKind` / `linkedTarget`；可确定解析的目标会额外写入
  `Identity.ResolvedLinkedTarget` 和 resolved-link debug metadata。
- split-package candidate 存在但加载失败时，Core inspection 会在对应目录
  stub 上输出稳定 warning diagnostic：`wcx.package.link.unresolved`。
- `.ini` / `LastWzIndex` 声明的 numbered shard 如果缺失或无法加载，Core
  inspection 会在 package root 上输出稳定 warning diagnostic：
  `wcx.package.group.shardMissing` / `wcx.package.group.shardInvalid`。
- PKG2 已有 KMST1199/1200 和 modern KMS directory/profile/offset 支持：
  synthetic fixture 锁住 `pkg2_kmst1200` 与 `pkg2_modern_kms` directory、
  CLI 可见输出和 image payload inspection；用户提供的本地 KMS/KMST
  smoke 验证了 root IMG 列表和 IMG extraction。完整 KMS 客户端下载后，再在
  统一 external client smoke 入口中补 KMS coverage。其他 PKG2 profile 仍返回
  稳定 error diagnostic：`wcx.package.pkg2.directoryUnsupported`。
- 本地 GMS 清点确认当前样本没有 PKG2、`List.wz` 或 `.mn`，但这些仍是
  旧客户端兼容目标；当前样本存在 `Data/Packs/*.ms`，`.ms` / `.mn` v2/Snow
  和 v4/ChaCha20 container directory inspection 已经接入 `inspect`，并且
  `.ms` / `.mn` image payload 可以通过同一条 IMG inspection 路径提取；
  `.mn` 目前由 synthetic fixture 覆盖，真实 `.mn` smoke 仍等待旧客户端样本。
  `List.wz` 已有基于 WC `LoadListWz` 的 first-slice reader，可通过
  `inspect` 观察 no-op/KMS/GMS list entries，但尚未接入 PKG1 key/profile
  选择；folder inspection 会跳过 `List.wz`，避免把 helper 文件误报为
  invalid WZ package；真实 smoke 同样等待旧客户端样本。
- Canvas preview 现在也能通过同一条 MS/MN payload 路径预览 `.ms` / `.mn`
  IMG 内的 direct Canvas，并能把 `.ms` IMG 内的 `_outlink` 按 WC-style
  logical path 解析到当前 MS container 或匹配的 WZ package group。
- 本地 GMS UI smoke 也覆盖了 `UI/UI_000.wz` / `Basic.img` 中
  `Cursor/0/0/_outlink` 到 `UI/_Canvas/_Canvas_000.wz` 的 Canvas preview
  解析路径。
- Canvas preview 的 direct-zlib viewer matrix 已覆盖 `BC7` (`4098`)。
  本地 GMS `Skill/_Canvas/_Canvas_097.wz` / `6414.img` smoke 验证了
  `skill/64141504/effect/1` 的 BC7 预览路径。
- Canvas preview 遇到无法解析的 `source` / `_inlink` / `_outlink` 目标时，
  现在会输出稳定 viewer error diagnostic：`wcx.viewer.canvas.linkUnresolved`，
  不再把 link 失败混同为普通 unsupported value。

后续短线工作：

- 继续按测试保护拆薄 `MainWindowViewModel` 的 activity / task-progress 编排。
- 继续复核 Core package group / split-package linking 的 tree composition 边界。
- 按 `docs/roadmap.md` 的 WC/WCX 能力矩阵推进 Milestone 5：先补解析覆盖
  和 resource identity，并用 `docs/parser-coverage-matrix.md` 跟踪 M5
  parser/resource-model 切片，再进入 Compare、Search、媒体导出和大型功能族。

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
- 外部客户端 smoke 只使用统一入口 `WCX_CLIENT_DATA_DIR`。
  `ResourceInspectionExternalClientSmokeTests` 会按目录中实际存在的文件能力运行
  代表性 WZ package、Map package group/link identity、`Data/Packs/*.ms`
  directory-table 和代表性 image payload inspection。
- TODO：等完整 KMS 客户端可用后，在同一个 external client smoke harness 里
  继续通过 `WCX_CLIENT_DATA_DIR` 补 KMS 覆盖；届时增加 modern PKG2 header
  variant、root IMG directory table、选中 IMG payload extraction，以及更多 KMS
  package families。
- M4 UI 行为测试放在 `WzComparerX.App.Tests`，不要塞进 Core tests。
- UI 可视回归优先使用 Avalonia Headless 和可选截图产物。
- 后续涉及 App/UI 或浏览器工作流的迭代，尽量额外跑 Headless 子集：
  `dotnet test tests/WzComparerX.App.Tests/WzComparerX.App.Tests.csproj --no-build -m:1 --filter FullyQualifiedName~Headless`。

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
