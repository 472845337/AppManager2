using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace AppManager2.Controls {
    /// <summary>
    /// TabControl表头面板：可见区为Tab序列上一段连续的"滑动窗口"（块），按MaxRows分行排布
    /// （1单行/2双行/0不限），窗口外的Tab收进溢出列表（配合OverflowTabControl的下拉箭头显示）。
    /// 选中隐藏Tab时窗口滑动露出它：目标在窗口右侧→右移到选中项成为右缘；
    /// 目标在窗口左侧→左移到选中项成为左缘（参考IntelliJ/VS2022的块式显隐）
    /// </summary>
    public class OverflowTabPanel : Panel {
        public static readonly DependencyProperty MaxRowsProperty = DependencyProperty.Register(
            "MaxRows", typeof(int), typeof(OverflowTabPanel),
            new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>最大行数：1单行、2双行、0不限（全部换行显示）</summary>
        public int MaxRows {
            get => (int)GetValue(MaxRowsProperty);
            set => SetValue(MaxRowsProperty, value);
        }

        /// <summary>溢出（窗口外被隐藏）的TabItem容器，按Tab原始顺序排列，布局后更新并触发事件</summary>
        public IReadOnlyList<UIElement> OverflowItems => _overflow;

        private readonly List<UIElement> _overflow = new List<UIElement>();
        private readonly List<List<UIElement>> _rows = new List<List<UIElement>>();
        /// <summary>可见窗口起始位置（第一个可见子项在子项序列中的索引），跨布局保留实现滑动</summary>
        private int _windowStart;
        public event EventHandler OverflowChanged;

        protected override Size MeasureOverride(Size availableSize) {
            _overflow.Clear();
            _rows.Clear();
            var maxRows = MaxRows > 0 ? MaxRows : int.MaxValue;
            var availableWidth = double.IsPositiveInfinity(availableSize.Width) ? double.MaxValue : availableSize.Width;
            if (InternalChildren.Count == 0) {
                _windowStart = 0;
                RaiseOverflowChangedIfNeeded();
                return new Size(0, 0);
            }

            // 期望宽度自由测量，行高取最高子项
            double rowHeight = 0;
            for (var i = 0; i < InternalChildren.Count; i++) {
                InternalChildren[i].Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                rowHeight = Math.Max(rowHeight, InternalChildren[i].DesiredSize.Height);
            }

            // 窗口起点合法性（Tab关闭后收缩）
            if (_windowStart > InternalChildren.Count - 1) {
                _windowStart = InternalChildren.Count - 1;
            }
            if (_windowStart < 0) {
                _windowStart = 0;
            }

            // 选中项必须落入窗口：目标在窗口右侧→右移使选中项成为右缘；
            // 在窗口左侧→左移使选中项成为左缘。容量随窗口起点变化，迭代至稳定
            var selectedIdx = FindSelectedIndex();
            for (var round = 0; round < 4; round++) {
                var capacity = ComputeVisibleCount(_windowStart, availableWidth, maxRows);
                if (selectedIdx < 0) break;
                int newStart;
                if (selectedIdx < _windowStart) {
                    newStart = selectedIdx;
                } else if (selectedIdx >= _windowStart + capacity) {
                    newStart = Math.Max(0, selectedIdx - capacity + 1);
                } else {
                    break;
                }
                if (newStart == _windowStart) break;
                _windowStart = newStart;
            }

            // 最终布局：从窗口起点贪心分行，窗口外的前后子项按原序进溢出
            var visibleCount = ComputeVisibleCount(_windowStart, availableWidth, maxRows);
            var end = Math.Min(_windowStart + visibleCount, InternalChildren.Count);
            for (var r = _windowStart; r < end;) {
                var row = new List<UIElement>();
                double rowWidth = 0;
                while (r < end) {
                    var child = InternalChildren[r];
                    var w = child.DesiredSize.Width;
                    if (row.Count > 0 && rowWidth + w > availableWidth) break;
                    row.Add(child);
                    rowWidth += w;
                    r++;
                }
                _rows.Add(row);
            }
            for (var i = 0; i < _windowStart; i++) {
                _overflow.Add(InternalChildren[i]);
            }
            for (var i = end; i < InternalChildren.Count; i++) {
                _overflow.Add(InternalChildren[i]);
            }

            RaiseOverflowChangedIfNeeded();
            return new Size(availableSize.Width, _rows.Count * rowHeight);
        }

        /// <summary>从start起贪心填充maxRows行，返回可容纳的子项数（宽度以贪心逐个累加为准）</summary>
        private int ComputeVisibleCount(int start, double availableWidth, int maxRows) {
            var count = 0;
            var rows = 0;
            double rowWidth = 0;
            var rowItems = 0;
            for (var i = start; i < InternalChildren.Count; i++) {
                var w = InternalChildren[i].DesiredSize.Width;
                if (rowItems > 0 && rowWidth + w > availableWidth) {
                    rows++;
                    if (rows >= maxRows) break;
                    rowWidth = 0;
                    rowItems = 0;
                }
                rowWidth += w;
                rowItems++;
                count++;
            }
            return count;
        }

        private int FindSelectedIndex() {
            for (var i = 0; i < InternalChildren.Count; i++) {
                if (InternalChildren[i] is TabItem tabItem && tabItem.IsSelected) {
                    return i;
                }
            }
            return -1;
        }

        protected override Size ArrangeOverride(Size finalSize) {
            var rowHeight = 0.0;
            foreach (UIElement child in InternalChildren) {
                rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            }
            var visible = new HashSet<UIElement>();
            for (var r = 0; r < _rows.Count; r++) {
                var x = 0.0;
                foreach (var child in _rows[r]) {
                    visible.Add(child);
                    child.Arrange(new Rect(x, r * rowHeight, child.DesiredSize.Width, rowHeight));
                    x += child.DesiredSize.Width;
                }
            }
            // 窗口外项排在零尺寸位置（不渲染、不占位）
            foreach (UIElement child in InternalChildren) {
                if (!visible.Contains(child)) {
                    child.Arrange(new Rect(0, 0, 0, 0));
                }
            }
            return finalSize;
        }

        /// <summary>溢出集合有实际变化才通知（Measure高频触发，避免频繁刷新下拉）</summary>
        private void RaiseOverflowChangedIfNeeded() {
            var changed = _previousOverflow.Count != _overflow.Count;
            if (!changed) {
                for (var i = 0; i < _overflow.Count; i++) {
                    if (!ReferenceEquals(_previousOverflow[i], _overflow[i])) {
                        changed = true;
                        break;
                    }
                }
            }
            if (!changed) return;
            _previousOverflow.Clear();
            _previousOverflow.AddRange(_overflow);
            OverflowChanged?.Invoke(this, EventArgs.Empty);
        }

        private readonly List<UIElement> _previousOverflow = new List<UIElement>();
    }
}
