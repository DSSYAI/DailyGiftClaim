<div align="center">

# Daily Gift One‑Click Claim

A small tool that claims your daily gift pack in VPet‑Simulator with one click — no more digging through the backpack every day.

[English](./README.md) | [简体中文](./README.zh.md)

</div>

---
**Steam 创意工坊**：[每日礼包一键领取](https://steamcommunity.com/sharedfiles/filedetails/?id=3785774469)
---

## Features

- **One‑click claim**  
  Open the inventory and click "Claim Daily Gift" on the toolbar to open all gift packs at once (including saved‑up ones).

- **Select & use**  
  Check multiple items in the inventory (with select‑all) and use them in one click — food is eaten, other items apply their effects.

- **Auto‑claim**  
  Enabled by default — claims automatically on game launch with a pet announcement; can be turned off in the inventory toolbar.

- **Cross‑midnight catch‑up**  
  Even if the game has been running across midnight without a restart, today's pack is still claimed automatically.

- **Official mechanism**  
  Opening reuses the game's own `Item.Use()` logic — rewards are identical to manual opening, and game updates require no changes.

---

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

---

*This mod was developed with assistance from DeepSeek Harness.*
