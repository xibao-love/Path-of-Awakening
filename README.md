# Path of Awakening

这是一个为了在不过多破坏平衡的情况下给游戏增添乐趣的 Mod。该 Mod 提供了多个独特的技能供玩家选择，每个技能都会带来截然不同的游戏体验和战术策略。

This mod adds multiple unique skills to enhance gameplay variety while maintaining game balance. Each skill offers a distinct playstyle.

---

## ⚠️ 重要提示 (IMPORTANT)

**本 Mod 需要房主（Host）与所有玩家（Clients）同时安装。**

**This mod requires the host and all clients to install it simultaneously.**

## 🎨 自定义UI背景

将想要作为背景的图片放入该Mod文件夹内，或直接放入 `plugins` 文件夹下，并将图片重命名为 `background.png`。

## 🎮 初始按键 

按下 F11 键可打开 UI 面板。按3使用主动技能。

# 🛠 核心系统：自定义快捷键 (Keybindings)

本 Mod 已集成 InputUtils。

你不再受限于固定键位，请直接在游戏的 Settings -> Change Keybinds 菜单中找到 `Path of Awakening` 分组，自由配置每个技能的触发按键。

---

# 🌟 技能详解 (Skill Details)

## 🔴 主动技能 (Active)

### 浴血奋战 (BloodlustSkill)

开启后造伤与承伤均翻倍。

初始 40s 持续时间，击杀怪可延长 5s（上限 80s）。

### 钢筋铁骨 (IronBonesSkill)

按下后获得 0.5s 短暂无敌。

若成功抵挡伤害：获 10s 爆发加速（130%）且无视体力，随后进入 5s 疲劳期（无法奔跑）。

### 魔术师 (MagicianSkill)

生成一把专属铲子。再次按下可收回。

限制： 持有期间无法使用其他武器，且武器不掉落。

### 灵魂共生 (SoulSymbiosisSkill)

对队友发起契约（每局限 1 次）。双方共担伤害、同生共死。

加成： 获 10% 永久移速，50m 内显示引路连线。无法接受外部治疗。

### 燃命愈伤 (LifeBurnHealSkill)

耗费自身 1% 血量/秒，转化为队友 2% 血量/秒。

代价： 治疗时双方均不可移动。使用后自身永久虚弱（-10% 移速/体力恢复）且禁疗。

### 冲刺爆发 (SprintBurstSkill)

10s 内移速提升至 150% 且无视体力，随后进入 5s 疲劳期。最后进入 5 分钟漫长冷却。

---

## 🔵 被动技能 (Passive)

### 血之活力 (BloodVitalitySkill) 可进阶]

体力耗尽时继续奔跑将抽取生命值。

滋养系统：

- 消耗 30 HP -> 滋养 I：永久 +10% 移速。
- 消耗 60 HP -> 滋养 II：永久 +20% 移速。
- 消耗 90 HP -> 滋养 III：永久 +30% 移速  +10% 体力恢复。

代价： 重伤自愈上限永久降为 10%。

### 厄运金手指 (UnluckyMidasSkill)

首次拾取未被拿过的废品，使其价值增加 50%。

代价： 你的最大生命值永久降至 20%。一碰就碎！

### 名刀司命 (MingDaoSkill)

抵挡致命伤害并保留 1 点生命（每局限 1 次）。触发后获 5s 爆发移速（150%）。

### 亡者遗怨 (DeadResentmentSkill)

死后化身自爆卡车。对周围 10m 造成爆炸伤害，5m内对怪物伤害为8点，对队友伤害为100%；5-10m内对怪物伤害为4点，对队友伤害为60%（和队友一起上天吧）。

### 平稳着陆 (SmoothLandingSkill)

从 5m 以上高处坠落，并获得 5s 加速且无视体力。随后疲劳3秒，冷却3分钟。

---

# 📦 安装要求 (Requirements)

为了让 Mod 正常工作，你需要安装以下前置：

1. [BepInExPack](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/)
2. [LethalCompany InputUtils](https://thunderstore.io/c/lethal-company/p/Rune580/LethalCompany_InputUtils/)

&nbsp;
