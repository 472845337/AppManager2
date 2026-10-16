using AppManager2.Commands;
using AppManager2.Models;
using AppManager2.Services;
using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Utils;

namespace AppManager2.ViewModels {
    /// <summary>
    /// 应用导出窗口VM（对应老项目 AppExportForm）
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public class AppExportViewModel {
        [AddINotifyPropertyChangedInterface]
        public class ExportItem {
            public string Section { get; set; }
            public string Title { get; set; }
            public bool IsChecked { get; set; }
        }

        public ObservableCollection<ExportItem> ExportApps { get; set; }

        private bool _selectAll = true;
        public bool SelectAll {
            get => _selectAll;
            set {
                _selectAll = value;
                foreach (var item in ExportApps) {
                    item.IsChecked = value;
                }
            }
        }

        public ICommand ExportCommand { get; set; }
        public ICommand RefreshCommand { get; set; }

        private readonly AppService _appService;
        private Window _window;

        public AppExportViewModel() {
            _appService = AppService.Instance;
            ExportApps = new ObservableCollection<ExportItem>();
            ExportCommand = new RelayCommand(OnExport);
            RefreshCommand = new RelayCommand(OnRefresh);
            LoadApps();
        }

        public void SetWindow(Window window) {
            _window = window;
        }

        private void LoadApps() {
            ExportApps.Clear();
            foreach (var section in _appService.GetAllSections()) {
                var app = _appService.GetAppBySection(section);
                if (app == null) continue;
                ExportApps.Add(new ExportItem {
                    Section = app.Section,
                    Title = app.Title,
                    IsChecked = true
                });
            }
        }

        private void OnRefresh(object obj) {
            LoadApps();
        }

        private void OnExport(object obj) {
            var checkedList = ExportApps.Where(i => i.IsChecked).Select(i => i.Section).ToList();
            if (checkedList.Count == 0) {
                MessageBox.Show("未选择导出数据！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var exportDic = new LinkedHashMap<string, AppRecord>();
            foreach (var section in checkedList) {
                var app = _appService.GetAppBySection(section);
                if (app == null) {
                    MessageBox.Show("数据已变更，请刷新后再试！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                exportDic.Add(section, app);
            }

            // 导出：JSON序列化后AES加密
            var content = JsonConvert.SerializeObject(exportDic);
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog {
                Filter = "Amp Files(*.amp)|*.amp",
                FileName = "AppConfig"
            };
            if (saveFileDialog.ShowDialog() != true) return;
            try {
                File.WriteAllText(saveFileDialog.FileName, AesUtils.Encrypt(content), new UTF8Encoding(false));
                MessageBox.Show("导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                _window?.Close();
            } catch (Exception ex) {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
