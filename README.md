# 每日礼包一键领取
《虚拟桌宠模拟器》每日礼包一键领取小工具 —— 不用再每天进背包手动开礼包。

## 用途
**一键领取**
打开背包，工具条点【一键领取每日礼包】即可一次领完背包中全部礼包（含多日积攒的）。

**批量勾选**
背包内可多选道具（支持全选）后一键使用——食物直接吃，其他道具按各自用途生效。

**自动领取**
默认开启，游戏启动时自动领取并桌宠播报；可在背包工具条关闭。

**跨零点补发**
游戏跨过午夜未重启时，也会自动领取当天的礼包。

**复用官方机制**
开包复用游戏自身的 `Item.Use()` 逻辑，奖励内容与手动开包完全一致，游戏更新也无需改动。

## 常见问题
**找不到按钮？**
首次使用需在 游戏设置 → MOD 管理 中允许本 MOD 的代码插件，然后重启游戏。

**会影响"干净存档"徽章吗？**
不会。所有操作均通过游戏官方 API 完成，与手动操作无任何区别。

**安全吗？**
纯本地运行：不联网、不上传数据、不注入、不改动其他进程，源码完全开源可供检查。

**卸载会丢道具吗？**
不会。已开出的道具属于正常游戏存档，删除 MOD 文件夹即可卸载。

**游戏更新后还能用吗？**
开包逻辑复用游戏自身代码，通常无需更新；如提示版本不兼容，等待作者更新即可。

***

# Daily Gift One‑Click Claim
A small tool that claims your daily gift pack in VPet‑Simulator with one click — no more digging through the backpack every day.

## Features
**One‑click claim**
Open the inventory and click "Claim Daily Gift" on the toolbar to open all gift packs at once (including saved‑up ones).

**Select & use**
Check multiple items in the inventory (with select‑all) and use them in one click — food is eaten, other items apply their effects.

**Auto‑claim**
Enabled by default — claims automatically on game launch with a pet announcement; can be turned off in the inventory toolbar.

**Cross‑midnight catch‑up**
Even if the game has been running across midnight without a restart, today's pack is still claimed automatically.

**Official mechanism**
Opening reuses the game's own `Item.Use()` logic — rewards are identical to manual opening, and game updates require no changes.

## FAQ
**Can't find the button?**
Enable the code plugin in Settings → MOD Manager, then restart the game.

**Does it affect the HashCheck badge?**
No. Everything goes through official game APIs, identical to manual play.

**Is it safe?**
100% local: no network, no data upload, no injection, no tampering with other processes. Fully open source for audit.

**Will uninstalling lose items?**
No. Claimed items are part of your normal save. Just delete the mod folder to uninstall.

**Will it survive game updates?**
Opening logic reuses the game's own code, so it usually keeps working; if a version warning appears, wait for the author to update.

*本 MOD 由 DeepSeek Harness 协助开发。 / This mod was developed with assistance from DeepSeek Harness.*
