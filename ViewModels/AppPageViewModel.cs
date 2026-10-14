using AppManager2.Commands;
using AppManager2.Config;
using AppManager2.Models;
using AppManager2.Services;
using AppManager2.Views;
using PropertyChanged;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace AppManager2.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class AppPageViewModel : BaseViewModel
    {
        private readonly AppService _appService;

        public ObservableCollection<TreeItemModel> AppTree { get; set; }
        public ObservableCollection<AppRunTabItemModel> RunningApps { get; set; }
        public TreeItemModel SelectedApp { get; set; }
        public int SelectedTabIndex { get; set; }

        public ICommand AddAppCommand { get; set; }
        public ICommand EditAppCommand { get; set; }
        public ICommand DeleteAppCommand { get; set; }
        public ICommand StartAppCommand { get; set; }
        public ICommand StopAppCommand { get; set; }
        public ICommand RefreshAppCommand { get; set; }
        public ICommand AllStartCommand { get; set; }
        public ICommand AllStopCommand { get; set; }
        public ICommand ClearAllCommand { get; set; }
        public ICommand CloseTabCommand { get; set; }

        public AppPageViewModel()
        {
            _appService = AppService.Instance;
            AppTree = new ObservableCollection<TreeItemModel>();
            RunningApps = new ObservableCollection<AppRunTabItemModel>();
            InitializeCommands();
            BuildAppTree();
            _appService.AppListChanged += BuildAppTree;
        }

        private void InitializeCommands()
        {
            AddAppCommand = new RelayCommand(OnAddApp);
            EditAppCommand = new RelayCommand(OnEditApp);
            DeleteAppCommand = new RelayCommand(OnDeleteApp);
            StartAppCommand = new RelayCommand(OnStartApp);
            StopAppCommand = new RelayCommand(OnStopApp);
            RefreshAppCommand = new RelayCommand(OnRefreshApp);
            AllStartCommand = new RelayCommand(OnAllStart);
            AllStopCommand = new RelayCommand(OnAllStop);
            ClearAllCommand = new RelayCommand(OnClearAll);
            CloseTabCommand = new RelayCommand(OnCloseTab);
        }

        private void OnAddApp(object obj)
        {
            var window = new AppFormWindow();
            window.Owner = System.Windows.Application.Current.MainWindow;
            window.SetOperateType(Configs.OperateTypeAdd);
            window.ShowDialog();
            // 保存成功后 AppService 会触发 AppListChanged，自动刷新 AppTree
        }

        /// <summary>
        /// 根据 AppService 缓存构建应用树：有分组的按分组归组，无分组的直接挂在根节点
        /// </summary>
        private void BuildAppTree()
        {
            AppTree.Clear();

            var sections = _appService.GetAllSections();
            var dic = _appService.GetAllSectionDic();
            var groupNodes = new System.Collections.Generic.Dictionary<string, TreeItemModel>();

            foreach (var section in sections)
            {
                var app = dic.TryGetValue(section, out var v) ? v : null;
                if (app == null) continue;

                var child = new TreeItemModel
                {
                    Name = app.Title,
                    Flag = "child",
                    Section = app.Section,
                    GroupName = app.Group,
                    RunStat = app.RunStat,
                    IsRunning = app.IsRunning,
                    IsWarn = app.IsWarn
                };

                if (string.IsNullOrEmpty(app.Group))
                {
                    AppTree.Add(child);
                }
                else
                {
                    if (!groupNodes.TryGetValue(app.Group, out var groupNode))
                    {
                        groupNode = new TreeItemModel { Name = app.Group, Flag = "group", GroupName = app.Group };
                        groupNodes[app.Group] = groupNode;
                        AppTree.Add(groupNode);
                    }
                    groupNode.Children.Add(child);
                }
            }
        }

        private void OnEditApp(object obj) { }
        private void OnDeleteApp(object obj) { }
        private void OnStartApp(object obj) { }
        private void OnStopApp(object obj) { }
        private void OnRefreshApp(object obj) { }
        private void OnAllStart(object obj) { }
        private void OnAllStop(object obj) { }
        private void OnClearAll(object obj) { }
        private void OnCloseTab(object obj) { }
    }
}
