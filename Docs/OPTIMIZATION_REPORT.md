# FPS Practice Demo 优化与验证报告

更新日期：2026-11-16  
工程：Unity 2021.3.8f1c1 / Built-in Render Pipeline / Legacy Input Manager

## 1. 模型与同步问题审查

对 Assets 中的 FBX、OBJ、Blender、Avatar、Animator、SkinnedMeshRenderer 和人物相关引用进行了检查：

- 项目没有人物模型、骨骼、Avatar 或角色动画资源，因此不存在可证实的“人物骨骼与模型不同步”。
- 原靶子使用 Unity primitive，外观老旧且缺乏细节。
- 第一人称模型常见的同步风险来自世界坐标追赶、父子层级错误、Update 顺序和分别计算人物/模型旋转。
- 新武器模型采用相机直接子节点，只修改局部坐标，并在 LateUpdate 中更新模型动画，避免漂移和视角不同步。

## 2. 新模型与材质

### 步枪 Vanguard Rifle

- 7,984 顶点、3,552 个三角形，约 1.0065 米长。
- 包含机匣、护木、通风槽、枪管、枪口、枪托、握把、弹匣、导轨和机械瞄具。
- 使用 Gunmetal、Polymer、Accent、Grip、Sight 五个子网格和对应 PBR 材质。

### 手枪 Precision Sidearm

- 3,888 顶点、1,740 个三角形，约 0.45 米长。
- 包含套筒、防滑纹、枪管、握把、弹匣底板、扳机护圈和瞄具。
- 与步枪共享五类材质槽，但保留独立网格和射击参数。

### 靶子

- 1,568 顶点、704 个三角形，直径 1.30 米，主体厚 0.16 米。
- 32 边倒角主体、共面同心色环、中心金靶和八颗浅六角螺栓。
- 使用 Outer、White、Red、Gold、Dark、Blue 六组材质。
- 使用与视觉网格一致的单个凸 MeshCollider；取消 Y=0.1 的旧缩放，改为真实尺寸网格。

所有模型均由共享程序化网格生成，全部材质槽非空、退化三角形为 0，坐标有效。新增 11 个模型专用 PBR 材质，启用 GPU Instancing。

## 3. 武器切换

- 数字键 1/2 直接选择步枪/手枪，Q 和滚轮循环切换。
- 切换装备时间为 0.25 秒，期间禁止射击。
- 每把武器独立保存弹匣、射速和换弹状态。
- 切换会取消当前换弹但保留余弹，切回不会错误补满弹药。
- 切换只激活/停用预建模型，不反复 Instantiate/Destroy。
- 模型支持装备抬升、视角摆动、移动摇摆和射击回弹。

默认配置：

| 武器 | 模式 | 射速 | 弹匣 | 换弹 | 扩散 |
| --- | --- | ---: | ---: | ---: | ---: |
| Vanguard Rifle | 全自动 | 600 RPM | 30 | 1.6 秒 | 0.12° |
| Precision Sidearm | 半自动 | 260 RPM | 12 | 1.0 秒 | 0° |

配置资产位于 `Assets/ScriptableObjects`，可继续新增 WeaponDefinition。

## 4. 靶子生成优化

旧式单平面、原位置随机方式已替换为可配置生成策略：

- Z=-5～-1 的纵深范围；
- 默认 1.5 米物理间距，适配新靶子直径；
- 默认 4° 最小视角间隔，减少相互遮挡；
- 命中后至少迁移 1.5 米，避免原地刷新；
- 0.2 秒延迟重生，降低瞬移感；
- 32 次随机尝试后执行三维网格兜底搜索；
- 空间不足时禁用目标，不静默生成重叠目标；
- 继续复用固定对象池，不在命中时创建/销毁目标。

## 5. 保留的既有修复

- 鼠标捕获帧不误射击。
- Player 与 Camera 分离，360° Yaw、±89° Pitch 和后坐力总角度限制。
- 重力、跳跃、冲刺、蹲伏、空中控制、头顶检测。
- 射速、弹药、手动/自动换弹、扩散和后坐力。
- Target(9)/Environment(10) 专用命中层，Player(8) 不参与自身命中。
- 独立 WeaponController、WeaponView、TargetSpawner、PracticeSession、PracticeHUD 和兼容外观 CreateObject。

## 6. 最终五轮验证

| 轮次 | 资源/静态检查 | 编译 | 核心测试 | 结果 |
| --- | ---: | --- | ---: | --- |
| 1 | 172 通过 | 通过（离线 Unity API 测试桩） | 10/10 | PASS |
| 2 | 172 通过 | 通过（离线 Unity API 测试桩） | 10/10 | PASS |
| 3 | 172 通过 | 通过（离线 Unity API 测试桩） | 10/10 | PASS |
| 4 | 172 通过 | 通过（离线 Unity API 测试桩） | 10/10 | PASS |
| 5 | 172 通过 | 通过（离线 Unity API 测试桩） | 10/10 | PASS |

累计：**5 次编译、860 项次资源/静态检查、50 项次核心逻辑测试，全部通过**。

核心测试实际调用模型工厂，验证步枪/手枪五个子网格、靶子六个子网格、顶点数量、所有材质槽三角形非空及坐标有效性。`git diff --check` 无空白错误。

复现：

```powershell
powershell -ExecutionPolicy Bypass -File Tools/validate-five-times.ps1
```

## 7. Unity 原生验证与限制

工程现有 **28 个 Unity 原生测试用例**，新增覆盖：

- 武器切换禁止射击和独立弹药；
- 模型相机父子关系及旋转后局部坐标无漂移；
- 武器模型实例复用和子网格数量；
- 程序化靶子网格与 MeshCollider 一致性；
- 既有移动、重力、目标池、场景和资源回归。

当前机器没有可用的 Unity 2021.3.8f1c1 编辑器，因此原生测试已写入但尚未执行。离线网格验证不能替代 Unity 中的材质、光照、裁剪、视角构图和实际射击画面验收。

请在 Unity 环境执行：

```powershell
powershell -ExecutionPolicy Bypass -File Tools/run-unity-tests.ps1 -UnityPath "<UnityEditor.exe 的完整路径>"
```

随后手动检查 1/2/Q/滚轮切换、武器弹药、模型构图、近裁剪、靶子遮挡、延迟重生和不同距离下的命中体验。
