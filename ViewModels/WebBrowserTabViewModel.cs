using AppManager2.Commands;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using PropertyChanged;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Utils;

namespace AppManager2.ViewModels {
    /// <summary>
    /// 浏览器单个标签VM：持有该标签全部有状态部分——地址文本、标题、WebView2实例。
    /// 视图里的标签内容是DataTemplate（切Tab会重建模板控件），但都绑定到本VM；
    /// WebView2实例经ContentPresenter原样呈现，页面/内核状态不丢失。
    /// 显式实现INotifyPropertyChanged：容器VM要订阅Address变化同步五角星，
    /// 而Fody织入的接口在编译期不存在，无法直接+=订阅
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public class WebBrowserTabViewModel : INotifyPropertyChanged {
        // Fody会在织入期为所有属性setter生成raise调用，编译期看似“从不使用”，属预期告警
#pragma warning disable CS0067
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067
        // 内核环境进程级共享：首个浏览页创建，后续浏览页复用（同一浏览器进程、同一用户数据目录）
        private static CoreWebView2Environment _sharedEnvironment;

        // 内核只初始化一次；失败后不再重试（避免每次切Tab都重新报错）；
        // 初始化中标志防止切Tab重入时并发调用EnsureCoreWebView2Async
        private bool _coreAttached;
        private bool _coreInitializing;
        private bool _coreFailed;

        public string Title { get; set; } = "新标签页";
        public string Address { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public bool HasError { get; set; }

        /// <summary>是否已成功发起过访问：未访问时聚焦地址栏方便直接输入</summary>
        public bool HasNavigated { get; private set; }

        /// <summary>主文档是否加载中：驱动Tab头的加载动画（NavigationStarting亮起、NavigationCompleted熄灭）</summary>
        public bool IsLoading { get; set; }

        /// <summary>当前地址是否已收藏：驱动地址栏五角星状态（容器VM在地址变化/收藏变化时回写）</summary>
        public bool IsFavorited { get; set; }

        /// <summary>用户正在地址栏输入：站内跳转回写地址时避免打断输入</summary>
        public bool AddressEditing { get; set; }

        public WebView2 BrowserControl { get; } = new WebView2();

        public RelayCommand NavigateCommand { get; }

        /// <summary>参数为收藏的url：回填到地址栏（不自动发起访问，回车才请求）</summary>
        public RelayCommand PickFavoriteCommand { get; private set; }

        public WebBrowserTabViewModel() {
            NavigateCommand = new RelayCommand(_ => Navigate());
            PickFavoriteCommand = new RelayCommand(PickFavorite);
            BrowserControl.SourceChanged += BrowserControl_SourceChanged;
            // 首次进入视觉树时初始化内核（TabControl按需加载Tab内容，不拖慢主窗口启动）
            BrowserControl.Loaded += BrowserControl_Loaded;
        }

        /// <summary>
        /// 创建CoreWebView2。用户数据目录固定在%LocalAppData%，兼容软件被放进Program Files的只读场景
        /// </summary>
        private async void BrowserControl_Loaded(object sender, RoutedEventArgs e) {
            if (!_coreAttached && !_coreFailed && !_coreInitializing) {
                _coreInitializing = true;
                try {
                    if (_sharedEnvironment == null) {
                        var userDataFolder = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "AppManager2", "WebView2");
                        _sharedEnvironment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                    }
                    await BrowserControl.EnsureCoreWebView2Async(_sharedEnvironment);
                    _coreAttached = true;
                    // 页面<title>变化→Tab头；并补发一次当前标题（Source排队发起的导航可能在此前已完成）
                    BrowserControl.CoreWebView2.DocumentTitleChanged += (s, a) => RefreshTitle();
                    RefreshTitle();
                    // 加载状态→Tab头动画；NavigationStarting/Completed只对应主文档（iframe是Frame*系列事件）
                    BrowserControl.CoreWebView2.NavigationStarting += (s, e) => IsLoading = true;
                    BrowserControl.CoreWebView2.NavigationCompleted += (s, e) => IsLoading = false;
                } catch (Exception ex) {
                    _coreFailed = true;
                    // 本机缺少WebView2运行时等场景：给出可理解的提示而不是一片空白
                    ErrorMessage = "浏览器内核初始化失败：本机需要安装Microsoft Edge WebView2运行时。" + ex.Message;
                    HasError = true;
                }
                _coreInitializing = false;
            }
        }

        private void RefreshTitle() {
            var title = BrowserControl.CoreWebView2.DocumentTitle;
            Title = string.IsNullOrWhiteSpace(title) ? "新标签页" : title;
        }

        private void BrowserControl_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e) {
            if (!AddressEditing) {
                Address = BrowserControl.Source?.ToString() ?? "";
            }
        }

        /// <summary>
        /// 请求地址栏输入的URL：无协议时按IP补http://、域名补https://（规则见HttpUtils.CompleteUrl）。
        /// 经Source赋值导航，内核未就绪时也会在初始化完成后自动补发
        /// </summary>
        private void Navigate() {
            if (string.IsNullOrEmpty(Address)) {
                MessageBox.Show("地址栏不能为空！");
                return; }
            Address = HttpUtils.CompleteUrl(Address);
            // 回车/前往即结束输入状态：键盘焦点仍留在地址栏也不会挡住后续redirect的地址同步
            AddressEditing = false;
            if (Uri.TryCreate(Address, UriKind.Absolute, out var uri)) {
                HasNavigated = true;
                // 内核未就绪时Source只是排队导航、不触发NavigationStarting，先亮出加载动画
                IsLoading = true;
                // 同一地址再次回车=刷新：给Source赋同值不会触发导航，动画会永远转下去
                if (uri.Equals(BrowserControl.Source)) {
                    BrowserControl.CoreWebView2?.Reload();
                } else {
                    BrowserControl.Source = uri;
                }
            }
        }

        /// <summary>点击收藏列表项：地址填入地址栏并直接发起访问，无需再回车/点前往</summary>
        private void PickFavorite(object param) {
            var url = param as string;
            if (string.IsNullOrEmpty(url)) return;
            Address = url;
            Navigate();
        }

        /// <summary>关闭标签页时释放本页内核资源（浏览器进程由共享环境管理，其他标签不受影响）</summary>
        public void CloseBrowser() {
            BrowserControl.Dispose();
        }
    }
}
