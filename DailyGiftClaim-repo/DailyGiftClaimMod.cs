using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LinePutScript.Localization.WPF;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace DailyGiftClaim
{
    /// <summary>
    /// 每日礼包一键领取 —— 代码插件入口
    /// <para>机制说明（与游戏本体 MainWindow.everydaygift / Item.UseAction 一致）：</para>
    /// <para>1. 游戏每天启动时向背包发放 1 个"每日礼包"(ItemType=Mail)；</para>
    /// <para>2. 打开礼包 = 调用 Item.Use()，由游戏自己的逻辑随机发放 3 个食物；</para>
    /// <para>3. 本插件复用 Item.Use()，因此游戏更新礼包内容后本插件无需改动。</para>
    /// <para>功能入口已集成到游戏背包窗口：一键领取 / 批量勾选 / 全选 / 一键使用。</para>
    /// </summary>
    public class DailyGiftClaimMod : MainPlugin
    {
        /// <summary>礼包物品名（与游戏内完全一致）</summary>
        private const string PackName = "每日礼包";

        /// <summary>本 mod 自己的设置行（游戏设置里自动新建，随设置存档保存）</summary>
        private const string SettingLine = "DailyGiftClaim";

        /// <summary>设置键：是否启动游戏时自动领取</summary>
        private const string SettingAutoClaim = "AutoClaim";

        /// <summary>设置键：最近一次自动领取的日期</summary>
        private const string SettingLastAutoDay = "LastAutoDay";

        /// <summary>背包工具条（懒创建，指向游戏背包窗口）</summary>
        private BackpackToolbar? _toolbar;

        /// <summary>MOD 名称，必须与 info.lps 中的 mod 名称完全一致（游戏按名称定位插件）</summary>
        public override string PluginName => "每日礼包一键领取";

        public DailyGiftClaimMod(IMainWindow mainwin)
            : base(mainwin) { }

        /// <summary>
        /// 插件加载：注册 EventTimer 定时器（跨零点自动领取 + 背包工具条注入检测）
        /// </summary>
        public override void LoadPlugin()
        {
            try
            {
                // 游戏每 15 秒触发一次 Elapsed（后台线程）
                MW.Main.EventTimer.Elapsed += OnTick;
            }
            catch (Exception ex)
            {
                MessageBox.Show("定时器注册失败：" + ex.Message, PluginName);
            }
        }

        /// <summary>
        /// 游戏加载完毕（此时每日礼包已发放到背包）：按设置自动领取（不弹窗，仅桌宠播报）
        /// </summary>
        public override void GameLoaded()
        {
            try
            {
                if (GetAutoClaim())
                {
                    // 每天只自动领一次，避免反复触发
                    var today = DateTime.Now.DayOfYear;
                    if (GetSettingInt(SettingLastAutoDay) == today)
                        return;
                    SetSettingInt(SettingLastAutoDay, today);

                    var gained = ClaimAll();
                    if (gained.Count > 0)
                        MW.Main.SayRnd("已帮你领取每日礼包！获得：" + string.Join("、", gained.Select(g => $"{g.Item.TranslateName} ×{g.Gained}")));
                }
            }
            catch (Exception ex)
            {
                // 自动领取失败不影响游戏运行，仅提示一次
                MessageBox.Show("自动领取失败：" + ex.Message, PluginName);
            }
        }

        /// <summary>
        /// 定时器（每 15 秒，后台线程触发）：整体切到 UI 线程执行，
        /// 避免与 GameLoaded 的自动领取并发读写 MW.Items / MW.Set 造成竞态。
        /// </summary>
        private void OnTick(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                MW.Dispatcher.Invoke(() =>
                {
                    TryAutoClaimMidnight();
                    EnsureBackpackHook();
                });
            }
            catch
            {
                // 定时器异常静默处理，不影响游戏（含游戏关闭瞬间 Dispatcher 失效的情况）
            }
        }

        /// <summary>跨零点自动领取（在 UI 线程上调用，不弹窗，仅桌宠播报）</summary>
        private void TryAutoClaimMidnight()
        {
            if (!GetAutoClaim())
                return;
            var today = DateTime.Now.DayOfYear;
            if (GetSettingInt(SettingLastAutoDay) == today)
                return; // 今天已自动领过
            SetSettingInt(SettingLastAutoDay, today);

            var gained = ClaimAll();
            if (gained.Count > 0)
                MW.Main.SayRnd("已帮你领取每日礼包！获得：" + string.Join("、", gained.Select(g => $"{g.Item.TranslateName} ×{g.Gained}")));
        }

        /// <summary>检测背包窗口（MainWindow.winInventory 属性），首次出现后挂接注入（UI 线程执行）</summary>
        private void EnsureBackpackHook()
        {
            try
            {
                MW.Dispatcher.Invoke(() =>
                {
                    var prop = MW.GetType().GetProperty("winInventory");
                    var value = prop?.GetValue(MW);
                    if (value is not Window win)
                        return; // 背包还没打开过（属性为空）

                    if (_toolbar != null && ReferenceEquals(_toolbar.Window, win))
                    {
                        _toolbar.EnsureInjected();
                        return;
                    }
                    _toolbar = new BackpackToolbar(this, win);
                    _toolbar.EnsureInjected();
                    // 背包窗口每次打开都会触发 Loaded（缓存实例重复 Show），每次重新注入
                    win.Loaded += (s, ev) =>
                    {
                        try { _toolbar?.EnsureInjected(); } catch { }
                    };
                });
            }
            catch
            {
                // 反射失败静默处理（游戏版本变化时），不影响自动领取
            }
        }

        /// <summary>播放领取动画（like520，与游戏本体 like520() 相同的调用）</summary>
        public void PlayClaimAnimation()
        {
            try
            {
                MW.Main.Display("like520", GraphInfo.AnimatType.Single, MW.Main.DisplayNomal);
            }
            catch
            {
                // 动画失败不影响领取
            }
        }

        /// <summary>
        /// 一键领取：确保今天的礼包已发放，再把背包里所有礼包一次性打开
        /// </summary>
        /// <returns>本次获得的物品（物品对象 + 本次获得数量）</returns>
        public List<(Item Item, int Gained)> ClaimAll()
        {
            // 领取前快照，用于计算"获得了什么"
            var before = MW.Items.ToDictionary(i => i.Name, i => i.Count);

            EnsureTodayPack();

            // 打开背包中所有礼包（每个 Count 用一次，直到用完或没有）
            for (int i = 0; i < 200; i++)
            {
                var pack = MW.Items.FirstOrDefault(x => x.Name == PackName && x.CanUse);
                if (pack is null)
                    break;
                int cntBefore = pack.Count;
                pack.Use(MW); // 复用游戏自己的打开逻辑（随机 3 个食物）
                if (pack.Count >= cntBefore)
                    break; // 没有消耗，防止死循环
            }

            // 领取后数量增加的物品 = 本次所得（ItemsAdd 自动合并，实例即背包中的那个）
            return MW.Items
                .Where(i => i.Count > before.GetValueOrDefault(i.Name))
                .Select(i => (Item: i, Gained: i.Count - before.GetValueOrDefault(i.Name)))
                .ToList();
        }

        /// <summary>
        /// 使用道具（复用游戏自己的 Use 动作，食物即吃、其他按用途生效）
        /// </summary>
        /// <param name="item">道具</param>
        /// <param name="count">使用数量</param>
        public void UseItem(Item item, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                if (item.Count <= 0)
                    break; // 已用光
                int before = item.Count;
                item.Use(MW);
                if (item.Count >= before)
                    break; // 没有消耗，防止死循环
            }
        }

        /// <summary>
        /// 确保今天的每日礼包已发放（与游戏 everydaygift 完全相同的逻辑，跨零点也能领到当天礼包）
        /// </summary>
        private void EnsureTodayPack()
        {
            var daily = MW.Set["dailydata"];
            var marker = daily.FindorAdd("everydaygift");
            if (marker.InfoToInt == DateTime.Now.DayOfYear)
                return; // 今天已发过

            marker.InfoToInt = DateTime.Now.DayOfYear;
            var itm = new Item
            {
                Name = PackName,
                Desc = "物品系统附赠的每日礼包, 打开后会获得3个随机物品.",
                ItemType = "Mail",
                Price = 15,
            };
            itm.LoadSource(MW);
            MW.ItemsAdd(itm);
        }

        /// <summary>背包中尚未打开的礼包总数（多个礼包会堆叠计数）</summary>
        public int PackCount => MW.Items.Where(x => x.Name == PackName).Sum(x => x.Count);

        /// <summary>今天是否已发放过礼包</summary>
        public bool TodayClaimed =>
            MW.Set["dailydata"].FindorAdd("everydaygift").InfoToInt == DateTime.Now.DayOfYear;

        #region 设置读写（使用游戏为 mod 预留的自定义设置行）

        public bool GetAutoClaim()
        {
            try
            {
                return MW.Set[SettingLine].FindorAdd(SettingAutoClaim).InfoToBoolean;
            }
            catch
            {
                return true; // 默认开启
            }
        }

        public void SetAutoClaim(bool value)
        {
            MW.Set[SettingLine].FindorAdd(SettingAutoClaim).InfoToBoolean = value;
        }

        private int GetSettingInt(string key)
        {
            try
            {
                return MW.Set[SettingLine].FindorAdd(key).InfoToInt;
            }
            catch
            {
                return 0;
            }
        }

        private void SetSettingInt(string key, int value)
        {
            MW.Set[SettingLine].FindorAdd(key).InfoToInt = value;
        }

        #endregion
    }
}
