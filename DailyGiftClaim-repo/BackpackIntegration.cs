using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LinePutScript.Localization.WPF;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace DailyGiftClaim
{
    /// <summary>
    /// 背包集成：向游戏背包窗口（winInventory）注入工具条
    /// 功能：一键领取每日礼包 / 批量勾选 / 全选 / 一键使用选中 / 自动领取开关
    /// 注：背包窗口是游戏私有 UI，通过反射注入，游戏大版本更新可能失效（届时需同步适配）。
    /// </summary>
    public class BackpackToolbar
    {
        private readonly DailyGiftClaimMod _mod;
        private readonly Window _win;
        private readonly HashSet<Item> _selected = new();
        private bool _batchMode;
        private bool _injected;
        private bool _hooked;
        private Button? _selectAllBtn;
        private Button? _useSelectedBtn;
        private TextBlock? _msgText;
        private CheckBox? _autoCheck;

        /// <summary>所注入的游戏背包窗口</summary>
        public Window Window => _win;

        /// <summary>
        /// 游戏常驻的 vup 初始道具（共 5 个）：不可正常使用/不需要使用，
        /// 批量选择（全选 / 点击勾选）时跳过。
        /// </summary>
        private static readonly HashSet<string> PermanentItems = new(StringComparer.Ordinal)
        {
            "L徽章", "逗猫棒", "泡泡枪", "球拍", "指南针",
        };

        private static bool IsPermanent(Item item) => item != null && PermanentItems.Contains(item.Name);

        public BackpackToolbar(DailyGiftClaimMod mod, Window win)
        {
            _mod = mod;
            _win = win;
        }

        /// <summary>确保工具条已注入（窗口每次打开都会调用）</summary>
        public void EnsureInjected()
        {
            if (_injected || _win.Content is not Grid root)
                return;
            try
            {
                // 在"分类"(行0) 与"排序"(行1) 与"物品网格"(行2) 之间插入工具条行
                root.RowDefinitions.Insert(2, new RowDefinition { Height = GridLength.Auto });
                foreach (var child in root.Children.OfType<UIElement>())
                    if (Grid.GetRow(child) == 2)
                        Grid.SetRow(child, 3); // 物品网格下移一行

                var bar = BuildToolbar();
                Grid.SetRow(bar, 2);
                root.Children.Add(bar);

                _injected = true;
                _win.Closed += (s, e) =>
                {
                    _injected = false;
                    _selected.Clear();
                    _batchMode = false;
                };
            }
            catch
            {
                // 注入失败静默处理（游戏界面结构变化时），不影响 mod 其他功能
            }
        }

        /// <summary>构建工具条 UI</summary>
        private UIElement BuildToolbar()
        {
            var bar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0xE3, 0xF2, 0xFD)),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(8, 6, 8, 6),
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };

            var claimBtn = new Button
            {
                Content = "一键领取每日礼包".Translate(),
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                FontWeight = FontWeights.Bold,
            };
            claimBtn.Style = MakeBlueStyle();
            claimBtn.Click += (s, e) => OnClaimClick();

            var batchBtn = new Button
            {
                Content = "批量选择".Translate(),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
            };
            batchBtn.Style = MakeGhostStyle();
            batchBtn.Click += (s, e) => ToggleBatch();

            _selectAllBtn = new Button
            {
                Content = "全选".Translate(),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                IsEnabled = false,
            };
            _selectAllBtn.Style = MakeGhostStyle();
            _selectAllBtn.Click += (s, e) => ToggleSelectAll();

            _useSelectedBtn = new Button
            {
                Content = "使用选中（{0} 种）".Translate(0),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                IsEnabled = false,
                FontWeight = FontWeights.Bold,
            };
            _useSelectedBtn.Style = MakeBlueStyle();
            _useSelectedBtn.Click += (s, e) => UseSelected();

            _autoCheck = new CheckBox
            {
                Content = "启动时自动领取".Translate(),
                FontSize = 13,
                IsChecked = _mod.GetAutoClaim(),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
            };
            _autoCheck.Checked += (s, e) => _mod.SetAutoClaim(true);
            _autoCheck.Unchecked += (s, e) => _mod.SetAutoClaim(false);

            _msgText = new TextBlock
            {
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x78, 0x90, 0x9C)),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };

            sp.Children.Add(claimBtn);
            sp.Children.Add(batchBtn);
            sp.Children.Add(_selectAllBtn);
            sp.Children.Add(_useSelectedBtn);
            sp.Children.Add(_autoCheck);
            sp.Children.Add(_msgText);
            bar.Child = sp;
            return bar;
        }

        private static Style MakeBlueStyle()
        {
            var s = new Style(typeof(Button));
            s.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x3C, 0xA9, 0xDB))));
            s.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            s.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x49, 0x99, 0xDA))));
            s.Triggers.Add(hover);
            return s;
        }

        private static Style MakeGhostStyle()
        {
            var s = new Style(typeof(Button));
            s.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.White));
            s.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x3C, 0xA9, 0xDB))));
            s.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            s.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0x81, 0xD4, 0xFA))));
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xBB, 0xDE, 0xFB))));
            s.Triggers.Add(hover);
            return s;
        }

        // ---------- 功能 ----------

        private void OnClaimClick()
        {
            try
            {
                _mod.PlayClaimAnimation();
                var gained = _mod.ClaimAll();
                if (gained.Count == 0)
                    _msgText.Text = "没有可领取的每日礼包～".Translate();
                else
                {
                    _msgText.Text = "已领取：".Translate() + string.Join("、", gained.Select(g => $"{g.Item.TranslateName} ×{g.Gained}"));
                    _mod.MW.Main.SayRnd("已帮你领取每日礼包！获得：" + string.Join("、", gained.Select(g => $"{g.Item.TranslateName} ×{g.Gained}")));
                }
                RefreshList();
            }
            catch (Exception ex)
            {
                _msgText.Text = "领取失败：" + ex.Message;
            }
        }

        private void ToggleBatch()
        {
            _batchMode = !_batchMode;
            if (!_batchMode)
            {
                _selected.Clear();
                UpdateSelectionVisuals();
            }
            EnsureHooked();
            UpdateButtons();
        }

        private void ToggleSelectAll()
        {
            var items = GetSelectableItems().ToList();
            bool all = items.Count > 0 && items.All(_selected.Contains);
            if (all)
                _selected.Clear();
            else
                foreach (var it in items)
                    _selected.Add(it);
            UpdateSelectionVisuals();
            UpdateButtons();
        }

        private void UseSelected()
        {
            if (_selected.Count == 0)
                return;
            try
            {
                foreach (var item in _selected.ToList())
                    _mod.UseItem(item, item.Count);
                _msgText.Text = "已使用".Translate();
                _selected.Clear();
                RefreshList();
            }
            catch (Exception ex)
            {
                _msgText.Text = "使用失败：" + ex.Message;
            }
        }

        private void UpdateButtons()
        {
            int n = _selected.Count;
            _useSelectedBtn!.Content = "使用选中（{0} 种）".Translate(n);
            _useSelectedBtn.IsEnabled = n > 0;
            var visible = GetSelectableItems().ToList();
            _selectAllBtn!.IsEnabled = _batchMode && visible.Count > 0;
            _selectAllBtn.Content = n > 0 && visible.All(_selected.Contains) ? "取消全选".Translate() : "全选".Translate();
        }

        // ---------- 列表刷新与勾选挂接 ----------

        /// <summary>刷新背包列表（调用游戏私有 UpdateList）并更新勾选状态</summary>
        public void RefreshList()
        {
            InvokeUpdateList();
            EnsureHooked();
            UpdateSelectionVisuals();
            UpdateButtons();
        }

        /// <summary>
        /// 在物品列表（IcCommodity）上挂一次全局勾选拦截。
        /// 用 ItemsControl 级的隧道事件（handledEventsToo），物品格子重建/刷新后依然生效。
        /// </summary>
        private void EnsureHooked()
        {
            if (GetField("IcCommodity") is not ItemsControl ic)
                return;
            if (!_hooked)
            {
                ic.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnCellPreview), true);
                _hooked = true;
            }
            UpdateSelectionVisuals();
        }

        private void OnCellPreview(object sender, MouseButtonEventArgs e)
        {
            if (!_batchMode)
                return; // 非批量模式：放行，走游戏原生的"显示详情"
            e.Handled = true; // 拦截，阻止打开详情
            if (e.OriginalSource is not DependencyObject src)
                return;
            var border = FindAncestorBorder(src);
            if (border?.DataContext is not Item item)
                return;
            if (IsPermanent(item))
                return; // 常驻初始道具：不参与批量选择
            if (!_selected.Remove(item))
                _selected.Add(item);
            UpdateSelectionVisuals();
            UpdateButtons();
        }

        private void UpdateSelectionVisuals()
        {
            if (GetField("IcCommodity") is not ItemsControl ic)
                return;
            for (int i = 0; i < ic.Items.Count; i++)
            {
                var container = ic.ItemContainerGenerator.ContainerFromIndex(i) as DependencyObject;
                var border = container == null ? null : FindCellBorder(container);
                if (border?.DataContext is not Item item)
                    continue;
                if (_selected.Contains(item))
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x49, 0x99, 0xDA));
                    border.BorderThickness = new Thickness(3);
                }
                else
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(0xB0, 0xD8, 0xF0));
                    border.BorderThickness = new Thickness(1);
                }
            }
        }

        private IEnumerable<Item> GetVisibleItems()
        {
            if (GetField("IcCommodity") is not ItemsControl ic)
                return Enumerable.Empty<Item>();
            return ic.Items.OfType<Item>();
        }

        /// <summary>可参与批量选择的道具（排除常驻初始道具）</summary>
        private IEnumerable<Item> GetSelectableItems() =>
            GetVisibleItems().Where(i => !IsPermanent(i));

        /// <summary>在容器视觉树中找物品格子的 Border（DataContext 为 Item）</summary>
        private static Border? FindCellBorder(DependencyObject root)
        {
            if (root is Border b && b.DataContext is Item)
                return b;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var r = FindCellBorder(VisualTreeHelper.GetChild(root, i));
                if (r != null)
                    return r;
            }
            return null;
        }

        /// <summary>从点击源向上找物品格子的 Border（DataContext 为 Item）</summary>
        private static Border? FindAncestorBorder(DependencyObject start)
        {
            DependencyObject? cur = start;
            while (cur != null)
            {
                if (cur is Border b && b.DataContext is Item)
                    return b;
                cur = VisualTreeHelper.GetParent(cur);
            }
            return null;
        }

        private void InvokeUpdateList()
        {
            _win.GetType().GetMethod("UpdateList", BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(_win, null);
        }

        private object? GetField(string name) =>
            _win.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_win);
    }
}
