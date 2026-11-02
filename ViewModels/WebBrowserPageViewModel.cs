using AppManager2.Commands;
using AppManager2.Models;
using AppManager2.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace AppManager2.ViewModels {
    /// <summary>
    /// 多标签浏览器容器VM：Tabs经ItemsSource绑定到TabControl，SelectedTab双向绑定选中标签。
    /// 每个标签一个WebBrowserTabViewModel；新增/关闭/选中切换都在集合与属性上操作，视图零逻辑。
    /// 收藏列表（Favorites）为整页共享：五角星收藏/取消写库后整体刷新，
    /// 标签地址变化时按库回写该标签的五角星状态
    /// </summary>
    public class WebBrowserPageViewModel : BaseViewModel {
        public ObservableCollection<WebBrowserTabViewModel> Tabs { get; } = new ObservableCollection<WebBrowserTabViewModel>();

        public WebBrowserTabViewModel SelectedTab { get; set; }

        /// <summary>收藏的URL列表：地址栏左侧三角的下拉数据源，所有标签共享</summary>
        public ObservableCollection<WebFavorite> Favorites { get; } = new ObservableCollection<WebFavorite>();

        public RelayCommand NewTabCommand { get; private set; }

        /// <summary>参数为被关标签的VM</summary>
        public RelayCommand CloseTabCommand { get; private set; }

        /// <summary>参数为保留标签的VM：关闭其余所有标签</summary>
        public RelayCommand CloseOtherTabsCommand { get; private set; }

        /// <summary>关闭全部标签</summary>
        public RelayCommand CloseAllTabsCommand { get; private set; }

        /// <summary>参数为标签VM：收藏/取消收藏该标签当前地址（写库，无确认提示）</summary>
        public RelayCommand ToggleFavoriteCommand { get; private set; }

        public WebBrowserPageViewModel() {
            NewTabCommand = new RelayCommand(_ => OpenTab());
            CloseTabCommand = new RelayCommand(CloseTab);
            CloseOtherTabsCommand = new RelayCommand(CloseOtherTabs);
            CloseAllTabsCommand = new RelayCommand(CloseAllTabs);
            ToggleFavoriteCommand = new RelayCommand(ToggleFavorite);
            ReloadFavorites();
            // 首次进入页面自动开一个空白标签页
            OpenTab();
        }

        private void OpenTab() {
            OpenTab(null);
        }

        /// <summary>新开标签页：url为空开空白页；header先用“新标签页”占位，页面加载出title后经PropertyChanged回填</summary>
        private void OpenTab(string url) {
            var tab = new WebBrowserTabViewModel();
            // 地址变化（加载完成/redirect回写）时同步该标签的五角星状态
            tab.PropertyChanged += HandleTabPropertyChanged;
            // 页面里target=_blank/window.open的新窗口请求→在本页新开一个标签加载
            tab.NewTabRequested += (s, targetUrl) => OpenTab(targetUrl);
            Tabs.Add(tab);
            SelectedTab = tab;
            if (!string.IsNullOrEmpty(url)) {
                tab.NavigateTo(url);
            }
        }

        // 方法名避开Fody的On属性名Changed通知钩子约定，防止被误认
        private void HandleTabPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName != nameof(WebBrowserTabViewModel.Address)) return;
            var tab = (WebBrowserTabViewModel)sender;
            tab.IsFavorited = WebFavoriteService.Instance.GetByUrl(tab.Address) != null;
        }

        /// <summary>收藏/取消收藏：按url整条增删，随后刷新下拉列表与所有标签的五角星</summary>
        private void ToggleFavorite(object param) {
            if (!(param is WebBrowserTabViewModel tab) || string.IsNullOrEmpty(tab.Address)) return;
            if (tab.IsFavorited) {
                WebFavoriteService.Instance.DeleteByUrl(tab.Address);
            } else {
                WebFavoriteService.Instance.Insert(new WebFavorite { Url = tab.Address, Title = tab.Title });
            }
            ReloadFavorites();
            RefreshFavoriteStars();
        }

        /// <summary>把所有标签的五角星刷成与库一致（收藏/取消后其它同址标签同步）</summary>
        private void RefreshFavoriteStars() {
            foreach (var tab in Tabs) {
                tab.IsFavorited = WebFavoriteService.Instance.GetByUrl(tab.Address) != null;
            }
        }

        private void ReloadFavorites() {
            Favorites.Clear();
            foreach (var favorite in WebFavoriteService.Instance.GetAll()) {
                Favorites.Add(favorite);
            }
        }

        private void CloseTab(object param) {
            if (param is WebBrowserTabViewModel tab) {
                CloseTab(tab);
            }
        }

        /// <summary>释放该页内核资源并从集合移除；关闭的是当前标签时选中项挪到相邻标签（优先右侧，边界时左侧）</summary>
        private void CloseTab(WebBrowserTabViewModel tab) {
            var index = Tabs.IndexOf(tab);
            var wasSelected = SelectedTab == tab;
            tab.CloseBrowser();
            Tabs.Remove(tab);
            // Remove触发选择变化后SelectedItem可能已被置空，需要补选相邻标签
            if (wasSelected && Tabs.Count > 0) {
                SelectedTab = Tabs[Math.Min(index, Tabs.Count - 1)];
            }
        }

        /// <summary>关闭除指定标签外的所有标签（保留标签的选中状态不动）</summary>
        private void CloseOtherTabs(object param) {
            if (!(param is WebBrowserTabViewModel keep)) return;
            // 集合在循环中被移除，先快照再遍历
            foreach (var tab in Tabs.ToArray()) {
                if (!ReferenceEquals(tab, keep)) {
                    CloseTab(tab);
                }
            }
        }

        /// <summary>关闭全部标签，内容区留空（只剩+号可再开）</summary>
        private void CloseAllTabs(object param) {
            foreach (var tab in Tabs.ToArray()) {
                CloseTab(tab);
            }
        }
    }
}
