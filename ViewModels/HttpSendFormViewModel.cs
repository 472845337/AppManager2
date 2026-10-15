using AppManager2.Commands;
using AppManager2.Models;
using AppManager2.Services;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Utils;

namespace AppManager2.ViewModels {
    /// <summary>
    /// 发送记录窗口VM（对应老项目HttpSendForm）：查看详情、删除、清空、去重（参数/链接/异常）、重发
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public class HttpSendFormViewModel : BaseViewModel {
        public ObservableCollection<HttpSend> HttpSendList { get; set; }

        [OnChangedMethod(nameof(OnSelectedChanged))]
        public HttpSend SelectedHttpSend { get; set; }

        public string DetailText { get; set; }

        public ICommand RefreshCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand ClearCommand { get; set; }
        public ICommand SendAgainCommand { get; set; }
        public ICommand DeduplicateUrlCommand { get; set; }
        public ICommand DeduplicateParamCommand { get; set; }
        public ICommand DeduplicateErrorCommand { get; set; }

        public HttpSendFormViewModel() {
            HttpSendList = new ObservableCollection<HttpSend>();

            RefreshCommand = new RelayCommand(OnRefresh);
            DeleteCommand = new RelayCommand(OnDelete);
            ClearCommand = new RelayCommand(OnClear);
            SendAgainCommand = new RelayCommand(OnSendAgain);
            DeduplicateUrlCommand = new RelayCommand(OnDeduplicateUrl);
            DeduplicateParamCommand = new RelayCommand(OnDeduplicateParam);
            DeduplicateErrorCommand = new RelayCommand(OnDeduplicateError);
            LoadData();
        }

        private void LoadData() {
            // SelectList按id倒序（最新在前），与老项目OrderBy="id desc"一致
            var list = HttpSendService.Instance.GetAllWithParams();
            HttpSendList.Clear();
            foreach (var item in list) {
                HttpSendList.Add(item);
            }
        }

        private void OnRefresh(object obj) {
            LoadData();
            DetailText = string.Empty;
        }

        /// <summary>
        /// 选中记录变化：构建详情文本（对应老项目HttpSendListBox_SelectedIndexChanged）
        /// </summary>
        private void OnSelectedChanged() {
            DetailText = BuildDetail(SelectedHttpSend);
        }

        private static string BuildDetail(HttpSend httpSend) {
            if (httpSend == null) return string.Empty;

            var sb = new StringBuilder();
            sb.Append($"请求:{httpSend.Url}").Append(Environment.NewLine);
            sb.Append($"结果:{httpSend.Result}").Append(Environment.NewLine);
            sb.Append($"时间:{httpSend.CreateTime}").Append(Environment.NewLine);
            sb.Append($"方式:{httpSend.Type}").Append(Environment.NewLine);
            sb.Append($"状态:{httpSend.Code}").Append(Environment.NewLine);
            sb.Append($"类型:{httpSend.ContentType}").Append(Environment.NewLine);

            var paramList = HttpSendParamService.Instance.GetByHttpSendId(httpSend.Id);
            var headParamList = paramList?.Where(p => HttpSendParam.TYPE_HEAD.Equals(p.Type)).ToList();
            var bodyParamList = paramList?.Where(p => HttpSendParam.TYPE_BODY.Equals(p.Type)).ToList();

            if (headParamList != null && headParamList.Count > 0) {
                sb.Append("Head").Append(Environment.NewLine);
                foreach (var param in headParamList) {
                    sb.Append($"    {param.ParamKey}").Append("=").Append(param.ParamValue).Append(Environment.NewLine);
                }
            }
            if (bodyParamList != null && bodyParamList.Count > 0) {
                sb.Append("参数").Append(Environment.NewLine);
                foreach (var param in bodyParamList) {
                    sb.Append($"    {param.ParamKey}").Append("=").Append(param.ParamValue).Append(Environment.NewLine);
                }
            }
            return sb.ToString();
        }

        private void OnDelete(object obj) {
            if (SelectedHttpSend == null) {
                MessageBox.Show("未选择待删除数据！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBoxResult.OK != MessageBox.Show("确认删除所选发送记录？", "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning)) {
                return;
            }
            DeleteRecord(SelectedHttpSend);
            LoadData();
            DetailText = "删除记录成功";
        }

        private static void DeleteRecord(HttpSend httpSend) {
            HttpSendService.Instance.Delete(httpSend.Id.Value);
            HttpSendService.Instance.Vacuum();
            HttpSendParamService.Instance.DeleteByHttpSendId(httpSend.Id);
        }

        private void OnClear(object obj) {
            if (MessageBoxResult.OK != MessageBox.Show("确认清空记录？", "警告", MessageBoxButton.OKCancel, MessageBoxImage.Warning)) {
                return;
            }
            HttpSendService.Instance.Clear();
            HttpSendService.Instance.Vacuum();
            HttpSendParamService.Instance.Clear();
            LoadData();
            DetailText = "清空记录成功";
        }

        /// <summary>
        /// 重发：回填到工具页并切到主窗口工具Tab（对应老项目SendAgainButton_Click）
        /// </summary>
        private void OnSendAgain(object obj) {
            if (SelectedHttpSend == null) {
                MessageBox.Show("未选择发送记录", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ToolPageViewModel.Instance?.LoadFromHistory(SelectedHttpSend);
            if (Application.Current.MainWindow is MainWindow main) {
                main.ShowMainWindow();
                main.SelectMainTab(2);
            }
        }

        #region 去重（对应老项目DeduplicateMenuStrip三项）

        /// <summary>
        /// 链接去重：Url相同保留最新的
        /// </summary>
        private void OnDeduplicateUrl(object obj) {
            var existDic = new Dictionary<string, bool>();
            foreach (var single in HttpSendService.Instance.SelectList()) {
                if (existDic.ContainsKey(single.Url)) {
                    DeleteRecord(single);
                } else {
                    existDic[single.Url] = true;
                }
            }
            LoadData();
            DetailText = "链接去重完成";
        }

        /// <summary>
        /// 参数去重：Url+方式+参数md5相同保留最新的
        /// </summary>
        private void OnDeduplicateParam(object obj) {
            var existDic = new Dictionary<string, bool>();
            foreach (var single in HttpSendService.Instance.SelectList()) {
                var existKey = GetHttpSendKey(single);
                if (existDic.ContainsKey(existKey)) {
                    DeleteRecord(single);
                } else {
                    existDic[existKey] = true;
                }
            }
            LoadData();
            DetailText = "参数去重完成";
        }

        private static string GetHttpSendKey(HttpSend httpSend) {
            var sb = new StringBuilder();
            sb.Append(httpSend.Url).Append("#").Append(httpSend.Type);
            if (!HttpUtils.HasRequestBody(httpSend.Type)) return sb.ToString();
            // 判断参数，form-data,form-urlencoded,raw
            var paramSb = new StringBuilder();
            paramSb.Append(httpSend.ContentType);
            var paramList = HttpSendParamService.Instance.GetByHttpSendId(httpSend.Id);
            if (paramList != null && paramList.Count > 0) {
                paramList.Sort((param1, param2) => param1.ParamKey.Equals(param2.ParamKey)
                    ? string.Compare(param1.ParamValue, param2.ParamValue, StringComparison.Ordinal)
                    : string.Compare(param1.ParamKey, param2.ParamKey, StringComparison.Ordinal));
                foreach (var p in paramList) {
                    paramSb.Append(p.ParamKey).Append("=").Append(p.ParamValue).Append("&");
                }
            }
            sb.Append("#").Append(Md5Utils.Md5Encrypt32(paramSb.ToString()));
            return sb.ToString();
        }

        /// <summary>
        /// 异常去重：去除所有非200的记录
        /// </summary>
        private void OnDeduplicateError(object obj) {
            foreach (var single in HttpSendService.Instance.SelectList()) {
                if (!"200".Equals(single.Code)) {
                    DeleteRecord(single);
                }
            }
            LoadData();
            DetailText = "异常去重完成";
        }

        #endregion
    }
}
