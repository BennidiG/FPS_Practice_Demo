# FPS Practice Demo

基于 **Unity 2021.3.8f1c1**、Built-in Render Pipeline 和 Legacy Input Manager 的第一人称射击训练场。

## 操作

| 输入 | 功能 |
| --- | --- |
| W / A / S / D | 按身体水平朝向移动，斜向速度归一化 |
| 鼠标 | 360° Yaw，±89° Pitch |
| 鼠标左键 | 当前武器射击；重新锁定鼠标的点击不会开火 |
| 鼠标右键 | ADS 瞄准，联动 FOV、武器位置和灵敏度 |
| 1 / 2 / Q / 滚轮 | 切换步枪 / 手枪 / 下一把武器 |
| Left Shift | 向前冲刺 |
| Space | 跳跃 |
| Left Ctrl / C | 蹲伏，头顶空间不足时无法站起 |
| R | 当前武器换弹，空匣自动换弹 |
| T / F4 | 重置当前训练 |
| F3 | 切换训练场景 |
| F6 | 打开/关闭训练总结；总结中 F3/F4 仍可使用 |
| F8 | 开关性能面板并清零统计 |
| Esc | 设置与暂停 |

## 阶段 0–2 已实现内容

### 训练时钟与暂停

`PracticeSession.TimeSeconds` 是统一训练时钟。换弹、射速、切枪、目标重生、动态轨迹、反应时间和 TTK 都使用该时钟。

打开设置或训练总结时，以下内容会真正暂停：

- 武器冷却和换弹；
- 切枪装备时间；
- 目标重生队列；
- 动态靶运动；
- 训练计时和反应/TTK 统计。

### ADS 与灵敏度

- 灵敏度以 **cm/360°** 表示，默认 30 cm。
- `DegreesPerCount = 360 / (cmPer360 × countsPerCentimeter)`。
- 默认 100 counts/cm，可在 `Initialization` 中根据 DPI/输入设备校准。
- ADS 默认 FOV 为 45°，灵敏度倍率为 0.65，武器移至居中瞄准位置。
- 后坐力同时影响相机和武器回弹，随机部分由训练种子决定，可复现。

### 训练场景

场景配置位于 `Assets/ScriptableObjects/Scenarios`：

| 场景 | 目标 | 轨迹 | 生命值 | 重生 |
| --- | --- | --- | ---: | --- |
| Static Flick | 6 | 静态 | 100 | 0.2 秒 |
| Dynamic Flick | 6 | 水平往返 | 100 | 0.20–0.35 秒 |
| Tracking | 3 | 平滑正弦 | 120 | 0.3 秒 |
| Reaction | 1 | 静态、随机出现 | 50 | 0.5–1.2 秒 |

每个场景具有独立随机种子，便于重复比较成绩。动态目标会检查环境和其他目标，避免穿墙与重叠。

### 命中区域与伤害

靶子按命中点到靶心的径向距离划分区域：

| 区域 | 半径 | 伤害倍率 | 分数 |
| --- | ---: | ---: | ---: |
| Bullseye | ≤0.10 | 2.00 | 100 |
| Critical | ≤0.24 | 1.50 | 75 |
| Inner | ≤0.38 | 1.25 | 50 |
| Outer | ≤0.52 | 1.00 | 25 |
| Rim | ≤0.59 | 0.75 | 10 |
| Frame | >0.59 | 0.50 | 5 |

步枪基础伤害 34，手枪基础伤害 50。目标不再必定一击消失；只有击杀后才进入重生队列。

### 训练指标与总结

实时和总结指标包括：

- 射击数、命中数、命中率、击杀数、分数和最佳连击；
- 首次命中反应时间；
- TTK（从目标出现到击杀）；
- 平均/最佳反应时间和 TTK；
- Bullseye、Critical、Inner、Outer、Rim、Frame 分区命中数。

### 声音与视觉反馈

- 两种枪声、换弹、切枪、命中和击杀音效，位于 `Assets/Audio`。
- 预建 8 个枪口/命中闪光对象，使用 `MaterialPropertyBlock`，不在射击时创建粒子或新材质实例。
- 武器支持装备抬升、摆动、移动摇摆、开火回弹、换弹下沉和 ADS 姿态。

## 靶场与移动

- 20×22×7 米室内靶场、四条靶道、挡弹背板、顶灯、控制台和凳子。
- CharacterController 支持重力、跳跃、冲刺、蹲伏、空中控制和头顶检测。
- Player 与 Camera 分离，武器为相机子节点，使用 LateUpdate 局部坐标，避免模型漂移。
- Target(9)/Environment(10) 为命中层，Player(8) 不参与自身命中。

## 训练数据、UI 与视觉升级

### 高级分析与历史

- 新增反应/TTK 中位数与 P95、鼠标路径效率、跟枪 on-target ratio。
- 最近 20 次训练自动保存到 `Application.persistentDataPath/FpsPractice`。
- 支持 JSON/CSV 导出、按场景的 PB 比较和历史查看。
- 使用一次性缓存反射兼容旧 Session API；实时数据每 0.2 秒刷新，避免逐帧重建 UI。

### uGUI 页面

新增 `TrainingUIController`，自动创建 Overlay Canvas、深色半透明面板和六个操作按钮：

- **Live**：场景、分数、命中率、反应/TTK 中位数与 P95、鼠标路径效率、跟枪比例。
- **Summary**：完整训练总结与高级指标。
- **History**：最近训练、PB、JSON/CSV 导出路径。
- **Help**：操作说明。

按钮支持 Start、Next、Reset、Summary、Export、Close。F7 循环 Live/History/Help，F6 仍由 Session 控制总结，F8 保留性能面板。旧 HUD 仅显示准星与性能信息，不重复显示训练数据。

### 手感、动画与特效

- 新增可复现的后坐力曲线，步枪与手枪具有不同节奏。
- 靶子命中后产生短促材质闪光，不改变真实碰撞体。
- 新增 24 个弹孔和 12 个弹壳的固定池；弹壳有简单抛射和旋转，弹孔定时回收。
- 新增径向弹孔贴图和黄铜弹壳材质，射击时不再创建临时物体。

## 验证

### 离线验证

```powershell
powershell -ExecutionPolicy Bypass -File Tools/validate-five-times.ps1
```

每轮包含：

1. 全部运行时代码编译。
2. 19 个核心逻辑测试，覆盖灵敏度、随机种子、轨迹、命中区域、反应/TTK、百分位数、鼠标路径、历史导出、后坐力和模型网格。
3. 283 项资源、场景、引用、配置、音频和静态检查。

微基准：

```powershell
powershell -ExecutionPolicy Bypass -File Tools/performance-benchmark.ps1
```

### Unity 原生验证

工程包含 **28 个原生测试用例**，包括：

- 暂停时换弹和 TrainingTime 停止；
- 动态靶随训练时钟冻结；
- ADS 武器居中；
- 武器切换与独立弹药；
- 模型父子关系和局部无漂移；
- 靶场只生成一次、灯光和设施碰撞；
- 既有目标池、重力、场景和模型回归。

使用 Test Runner 或：

```powershell
powershell -ExecutionPolicy Bypass -File Tools/run-unity-tests.ps1 -UnityPath "<UnityEditor.exe 的完整路径>"
```

当前机器没有可用的 Unity 编辑器，因此原生测试已写入但尚未执行。离线检查不能替代 Unity 中的输入、渲染、音频、物理和实际性能验收。

性能面板使用 F8，详细数据见 `Docs/PERFORMANCE_REPORT.md`。靶场说明见 `Docs/RANGE_ENVIRONMENT.md`。
