# 2D Top-Down Multiplayer Shooter (2D 俯视角多人联机射击游戏)

![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity)
![C#](https://img.shields.io/badge/C%23-Language-blue?logo=csharp)
![NGO](https://img.shields.io/badge/Network-NGO-brightgreen)
![Relay](https://img.shields.io/badge/Server-Unity_Relay-orange)

## 项目简介 | Overview

本项目是一款基于 Unity 和 **Netcode for GameObjects (NGO)** 开发的 2D 俯视角多人联机射击游戏。游戏支持局域网直连与**云端全球匹配（Unity Relay）**，玩家可以通过 6 位邀请码快速组局对战。

项目中深入实践了现代多人游戏网络架构，重点攻克了**高网络延迟下的射击手感优化（客户端预测）**、**高频生成子弹的性能瓶颈（对象池机制）**，以及基于 NavMesh 的战术 AI 寻路系统。

##  核心技术 | Key Technical Features

### 1. 全球直连网络架构 (Relay + NGO)
- 采用 **Host-Client (权威服务器)** 架构，防止客户端作弊（如锁血、秒杀）。
- 深度集成 **Unity Relay** 云端中继服务与匿名身份认证（Anonymous Auth），完美穿透 NAT 限制，无需公网 IP 即可实现跨网段稳定联机。
- 支持基于 6 位动态邀请码的房间创建与加入系统。

###  2. 客户端预测与表现逻辑分离 (Client-Side Prediction)
为了解决传统网络射击游戏中“开枪延迟”的手感痛点，本项目实现了**表现层与逻辑层的彻底分离**：
- **表现层（视觉子弹）：** 玩家按下开火键时，客户端即刻在本地生成“假子弹”（仅包含拖尾、碰撞火花特效），提供 **0 延迟**的跟手反馈。
- **逻辑层（物理子弹）：** 开火指令通过 RPC 同步至服务端，由服务端生成带有真实 Collider 的“真子弹”（隐藏渲染），负责绝对权威的碰撞检测与伤害结算（`TakeDamage`）。

###  3. 高性能子弹对象池 (Object Pool Optimization)
针对射击游戏同屏高频生成/销毁子弹导致的 GC（垃圾回收）卡顿问题，独立封装了支持网络环境的 `ObjectPoolManager`。
- 实现 `isLogicBullet` 与 `isAIBullet` 的多重状态重置与回收。
- 引入**协程超时自动回收**与**碰撞立即回收**双重保险，彻底消灭 `Instantiate` 与 `Destroy` 带来的内存碎片，极大提升了同屏混战的帧率稳定性。

###  4. 战术 AI 与动态寻路 (FSM + NavMesh)
- 基于 Unity **NavMesh Surface** 实现 2D 平面的网格烘焙与智能避障。
- 构建了基于状态机（FSM）的 AI 大脑（`TurretBrainMega`），包含三种无缝切换的状态：
  - **Patrol (巡逻)：** 在出生点周围随机半径内动态索敌。
  - **Chase (追击)：** 发现玩家后全速追踪并保持压迫感。
  - **Attack (攻击)：** 结合玩家当前速度与子弹飞行时间，实现**动态预判瞄准 (Lead Aiming)**，极大地提升了 PVE 的挑战性。
- 利用 Cinemachine 实现大地图边界约束下的平滑运镜。

###  5. 未来展望
- 目前客户端延迟过高的时候就会有视觉子弹打中人但不出伤害，为了修复这一体验手感，准备加入延迟补偿机制。

##  如何运行 | Getting Started

1. 确保已安装 **Unity 2022.3** 或更高版本。
2. 克隆本仓库到本地：
   ```bash
   git clone https://github.com/Wadu76/WADU_TOP_DOWN_2D.git
   ```
