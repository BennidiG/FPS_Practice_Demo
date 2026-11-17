# 训练数据、UI 与手感/视觉升级报告

日期：2026-11-17

## 已实现

### 高级训练分析

- 反应时间/TTK 的中位数与 P95，使用复制排序和线性插值。
- 鼠标路径效率：朝目标收敛的有效角位移 / 总鼠标角位移。
- 跟枪 on-target ratio：准星位于目标 3° 范围内的时间占比。
- 最近 20 条训练历史、按场景比较 PB。
- JSON/CSV 导出，CSV 使用 InvariantCulture 和字段转义。
- 历史自动保存到 `Application.persistentDataPath/FpsPractice`。
- 损坏历史文件不会直接清空当前内存记录。

### uGUI

`TrainingUIController` 自动创建 Overlay Canvas、CanvasScaler、GraphicRaycaster、深色半透明页面及六个按钮：

- Live：实时训练场景、命中率、分数、反应/TTK 中位数与 P95、鼠标路径和跟枪指标。
- Summary：训练总结和高级指标。
- History：最近训练、PB 和最新导出目录。
- Help：操作说明。

按钮包括 Start、Next、Reset、Summary、Export、Close。F7 切换 Live/History/Help，F6 切换总结，F8 保留性能面板。旧 PracticeHUD 仅保留准星和性能数据。

### 后坐力与动画

- 新增可复现的 `RecoilPattern`，包含垂直递增/回落和水平交替曲线。
- 步枪与手枪使用不同后坐力倍率。
- 每把武器独立记录连续射击序号，重置训练时统一复位。
- ADS 继续降低扩散和后坐力。

### 靶子与特效

- 命中目标触发短促材质闪光，使用 MaterialPropertyBlock，不修改真实碰撞体。
- 24 个弹孔和 12 个弹壳固定池，循环复用。
- 弹壳具有简单抛射、重力与旋转；弹孔定时回收。
- 新增径向弹孔贴图、透明材质和黄铜弹壳材质。
- 开火、命中、击杀、换弹与切枪继续使用预建闪光对象及音效。

### 导出与历史

训练结束或手动导出时写入：

- `training-history.json`
- `training-history.csv`

不在每发子弹时写磁盘。导出失败时 UI 不再误报完成。

## 最终五轮离线验证

| 轮次 | 资源/静态检查 | 编译 | 核心测试 | 结果 |
| --- | ---: | --- | ---: | --- |
| 1 | 283 通过 | 通过 | 19/19 | PASS |
| 2 | 283 通过 | 通过 | 19/19 | PASS |
| 3 | 283 通过 | 通过 | 19/19 | PASS |
| 4 | 283 通过 | 通过 | 19/19 | PASS |
| 5 | 283 通过 | 通过 | 19/19 | PASS |

累计：**5 次编译、1,415 项次检查、95 项次核心逻辑测试，全部通过**。

新增验证覆盖百分位、鼠标路径效率、PB 选择、CSV/JSON 和后坐力曲线边界。

## 微基准

新增模块接入后微基准仍正常：

- Rifle 约 0.472 ms/build。
- Sidearm 约 0.209 ms/build。
- Target visual 约 0.122 ms/build。
- Target collision 约 0.018 ms/build。
- Range environment 约 0.835 ms/build。
- WeaponState 约 5.5 ns/call，1,000,000 次操作约 40 bytes 累计分配。

微基准不包含 Unity 物理、GPU、渲染或真实帧时间。

## 验证边界与后续工作

当前机器没有 Unity 2021.3.8f1c1 编辑器，因此 **27 个 Unity 原生测试已写入但尚未执行**：

- uGUI 页面与按钮数量、重复创建检查；
- 弹孔/弹壳固定池复用；
- 暂停时换弹和动态轨迹；
- ADS、模型同步和既有玩法回归。

仍需在 Unity 中验收：

1. uGUI 字体、布局、滚动和不同分辨率显示。
2. ADS 真实瞄具对齐、武器穿模和后坐力手感。
3. 弹孔方向、弹壳落地、音频与材质表现。
4. persistentDataPath 实际写入权限及 JSON/CSV 内容。
5. Unity Profiler 下真实 CPU/GPU/PhysX/GC 表现。

本次没有声称完成人工角色手臂、完整骨骼换弹动画、全套 PBR 贴图或光照烘焙。若继续追求商业级视觉，应在此基础上增加正式模型/UV/法线材质、第一人称手臂动画和室内间接光烘焙。
