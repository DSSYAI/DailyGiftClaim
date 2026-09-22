# 每日礼包一键领取 / Daily Gift One-Click Claim

《虚拟桌宠模拟器》的每日礼包一键领取工具 —— 全自动领取 + 背包内批量管理。
A small tool for VPet-Simulator that claims your daily gift pack automatically and lets you bulk-manage items inside the inventory.

**Steam 创意工坊**：[每日礼包一键领取](https://steamcommunity.com/sharedfiles/filedetails/?id=3785774469)

---

## 功能 / Features

- **全自动领取**：启动游戏即自动开完所有"每日礼包"，桌宠播报所得（可关闭）。
- **跨零点自动领**：游戏挂机跨过午夜也会自动补发并领取当天的礼包。
- **背包内一键领取**：打开背包，工具条点【一键领取每日礼包】，一次开完所有礼包（含多日积攒的）。
- **批量勾选**：背包内可多选/全选道具后【一键使用】——食物直接吃，其他按各自用途生效（自动跳过常驻的 5 个 vup 初始道具）。
- **复用官方机制**：开包调用游戏自己的 `Item.Use()`，奖励内容与手动开包完全一致，游戏更新也无需改动。

## 安装 / Install

- **玩家**：Steam 创意工坊订阅本 MOD → 首次启动如提示"包含代码插件"→ 到 **游戏设置 → MOD 管理** 允许本 MOD 的代码插件 → 重启游戏。打开背包即可看到工具条。
- **本地测试**：把构建产物 `发布/DailyGift` 整个文件夹复制到 `<游戏目录>\mod\DailyGift\`。

> ⚠️ 代码插件需要手动允许一次，这是游戏对所有未签名代码插件的官方安全机制，一次性操作。

## 从源码构建 / Build from source

1. 安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（Windows x64）。
2. 在本目录执行：
   ```
   powershell -ExecutionPolicy Bypass -File .\publish.ps1
   ```
3. 产物在 `发布\DailyGift\`（info.lps + icon.png + lang/ + plugin/）。

构建需要联网恢复 NuGet 依赖（VPet-Simulator.Core / VPet-Simulator.Windows.Interface）。

## 文件结构 / Layout

```
├── DailyGiftClaim.csproj   # 工程文件 (.NET 8 / WPF)
├── DailyGiftClaimMod.cs    # 插件入口：自动领取 / 跨零点定时 / 背包窗口检测
├── BackpackIntegration.cs  # 背包集成：工具条注入 / 批量勾选 / 全选 / 一键使用
├── info.lps                # MOD 清单（多语言）
├── icon.png                # 封面图标
├── publish.ps1             # 一键构建打包脚本
├── lang/en/DailyGift.lps   # 英文翻译
└── docs/                   # 创意工坊文案
```

## 工作原理 / How it works

- 游戏每天启动时调用 `everydaygift()`：往背包发放 1 个 `每日礼包`（`ItemType=Mail`），并用 `Set["dailydata"]["everydaygift"]` 记录当天已发。
- 打开礼包 = `Item.Use()` → 游戏注册的 `UseAction["Mail"]` → 按宠物等级随机 `ItemsAdd` 3 个食物。
- 本 MOD：
  - `GameLoaded()` / 15 秒 `EventTimer`：自动领取（UI 线程串行，避免竞态），`LastAutoDay` 标记保证每天只领一次、跨零点自动补发。
  - 背包集成：反射 `MainWindow.winInventory` 属性拿到背包窗口，注入工具条；批量勾选用 ItemsControl 级隧道事件拦截点击（游戏刷新列表后依然生效）。

> 注意：背包窗口是游戏私有 UI，依赖反射注入；游戏大版本更新后若工具条未出现，属正常现象，等待更新适配。

## 常见问题 / FAQ

- **打开背包没看到工具条？** 确认已允许代码插件并重启游戏；工具条检测最长延迟约 15 秒（或重开一次背包）。
- **会影响"干净存档"徽章吗？** 不会。所有操作走游戏官方 API，与手动操作无区别。
- **安全吗？** 纯本地运行，不联网、不上传数据、不注入、不改动其他进程，本仓库源码即全部代码，可自行审计。
- **游戏更新后还能用吗？** 开包逻辑复用游戏自身代码，通常无需更新；如提示版本不兼容，把 `info.lps` 的 `gamever` 更新为游戏当前版本。

## 致谢 / Credits

本 MOD 由 DeepSeek Harness 协助开发。
This mod was created with the assistance of DeepSeek Harness.

## 开源许可 / License

[MIT](LICENSE)
