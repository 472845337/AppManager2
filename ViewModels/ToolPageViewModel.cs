using AppManager2.Commands;
using AppManager2.Config;
using AppManager2.CustomEnum;
using AppManager2.Excep;
using AppManager2.Models;
using AppManager2.Services;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Utils;

namespace AppManager2.ViewModels
{
    /// <summary>
    /// 工具页VM（对应老项目FormToolEvent的PostDog页）
    /// URL行 + Head参数区 + Form/Raw参数区 + 响应区，分隔高度持久化到System.ini [tool]
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public class ToolPageViewModel : BaseViewModel
    {
        /// <summary>raw参数类型下拉选项（与老项目RawTypeEnum枚举名一致）</summary>
        public static readonly string[] RawTypes = { "Text", "JavaScript", "Json", "Html", "Xml" };

        /// <summary>请求方式下拉选项（常量取自HttpUtils，历史记录Type回填时可直接匹配）</summary>
        public static readonly string[] HttpTypes =
        {
            HttpUtils.TypeGet, HttpUtils.TypePost, HttpUtils.TypePut, HttpUtils.TypePatch,
            HttpUtils.TypeDelete, HttpUtils.TypeHead, HttpUtils.TypeOptions
        };

        public string HttpUrl { get; set; }

        [OnChangedMethod(nameof(OnHttpTypeChanged))]
        public string HttpType { get; set; } = HttpUtils.TypeGet;

        /// <summary>是否可携带请求体的方式（决定Expect100Continue/Form参数等控件可用性，由HttpType变化联动）</summary>
        public bool HasBody { get; private set; }

        public bool Expect100Continue { get; set; } = true;

        // 超时设置（即时持久化，对应老项目SendUrlTimeOutCheckbox/TextBox）
        public bool IsTimeoutEnabled { get; set; }
        public string TimeoutText { get; set; } = Configs.HttpDefaultTimeout.ToString();

        // 是否自动记录请求（即时持久化）
        public bool SaveHttpRequest { get; set; }

        // 参数区
        public int ParamModeIndex { get; set; } // 0=Form 1=Raw
        public bool FormIsFormData { get; set; } = true; // form-data / x-www-form-urlencoded
        public ObservableCollection<HttpFormParamItem> FormParams { get; set; }
        public string RawType { get; set; } = "Json";
        public string RawBody { get; set; }

        // Head区
        public ObservableCollection<HttpParamItem> HeadParams { get; set; }

        // 响应
        public string HttpResponse { get; set; }

        // 发送/停止按钮状态（对应老项目_type = send/stop）
        public string SendButtonText { get; set; } = "发送";
        private HttpWebRequest _currentRequest;

        // 分隔位置百分比0-100（对应老项目HttpToolSplitContainer/HeadBodySplitContainer的SplitterDistance；
        // 绝对像素在不同窗口尺寸下会造成子页面溢出，故改按比例记忆，ini [tool]持久化）
        public double ParamSplitPercent { get; set; } = 50;   // 参数区占下方区域百分比（split-pct）
        public double HeadSplitPercent { get; set; } = 40;    // Head区占整页百分比（split-pct-head）

        public ICommand SendHttpRequestCommand { get; set; }
        public ICommand SaveHttpConfigCommand { get; set; }
        public ICommand ViewHttpHistoryCommand { get; set; }
        public ICommand AddParamCommand { get; set; }
        public ICommand DeleteParamCommand { get; set; }
        public ICommand ClearAllParamCommand { get; set; }
        public ICommand BrowseParamFileCommand { get; set; }
        public ICommand AddHeaderCommand { get; set; }
        public ICommand DeleteHeaderCommand { get; set; }
        public ICommand ClearAllHeaderCommand { get; set; }
        public ICommand ClearResponseCommand { get; set; }

        public ToolPageViewModel()
        {
            Instance = this;
            FormParams = new ObservableCollection<HttpFormParamItem>();
            HeadParams = new ObservableCollection<HttpParamItem>();

            SendHttpRequestCommand = new RelayCommand(OnSendHttpRequest);
            SaveHttpConfigCommand = new RelayCommand(OnSaveHttpConfig);
            ViewHttpHistoryCommand = new RelayCommand(OnViewHttpHistory);
            AddParamCommand = new RelayCommand(OnAddParam);
            DeleteParamCommand = new RelayCommand(OnDeleteParam);
            ClearAllParamCommand = new RelayCommand(OnClearAllParam);
            BrowseParamFileCommand = new RelayCommand(OnBrowseParamFile);
            AddHeaderCommand = new RelayCommand(OnAddHeader);
            DeleteHeaderCommand = new RelayCommand(OnDeleteHeader);
            ClearAllHeaderCommand = new RelayCommand(OnClearAllHeader);
            ClearResponseCommand = new RelayCommand(OnClearResponse);

            AddFormParam();
            AddHeadParam();
            LoadToolSettings();
        }

        /// <summary>单例入口，供发送记录窗口“重发”回填使用</summary>
        public static ToolPageViewModel Instance { get; private set; }

        #region 布局与选项持久化（对应老项目InitToolPage + 分隔器移动事件）

        private void LoadToolSettings()
        {
            var iniData = IniParserUtils.GetIniData(Configs.AppPath + "\\" + Configs.SystemIniPath);
            var splitPct = iniData[Configs.IniSectionTool][Configs.IniKeyToolSplitPct];
            var headSplitPct = iniData[Configs.IniSectionTool][Configs.IniKeyToolSplitPctHead];
            if (!string.IsNullOrEmpty(splitPct) && double.TryParse(splitPct, out var d1))
            {
                ParamSplitPercent = ClampSplitPercent(d1);
            }
            if (!string.IsNullOrEmpty(headSplitPct) && double.TryParse(headSplitPct, out var d2))
            {
                HeadSplitPercent = ClampSplitPercent(d2);
            }
            var saveHttpRequest = iniData[Configs.IniSectionTool][Configs.IniKeySaveHttpRequest];
            SaveHttpRequest = !string.IsNullOrEmpty(saveHttpRequest) && Convert.ToBoolean(saveHttpRequest);
            var isTimeout = iniData[Configs.IniSectionTool][Configs.IniKeyToolIsTimeout];
            IsTimeoutEnabled = bool.TrueString.Equals(isTimeout);
            var timeout = iniData[Configs.IniSectionTool][Configs.IniKeyToolTimeout];
            if (!string.IsNullOrEmpty(timeout))
            {
                TimeoutText = timeout;
            }
        }

        /// <summary>分隔百分比合法区间：过小/过大都会挤没另一侧</summary>
        public const double SplitPercentMin = 10;
        public const double SplitPercentMax = 90;

        private static double ClampSplitPercent(double value)
        {
            return Math.Max(SplitPercentMin, Math.Min(SplitPercentMax, value));
        }

        /// <summary>分隔器拖动结束后由View调用，保存两处分隔百分比（对应老项目SplitterMoved）</summary>
        public void SaveSplitHeights()
        {
            var iniPath = Configs.AppPath + "\\" + Configs.SystemIniPath;
            IniParserUtils.SaveIniData(iniPath, Configs.IniSectionTool, Configs.IniKeyToolSplitPct, Convert.ToInt32(ClampSplitPercent(ParamSplitPercent)).ToString());
            IniParserUtils.SaveIniData(iniPath, Configs.IniSectionTool, Configs.IniKeyToolSplitPctHead, Convert.ToInt32(ClampSplitPercent(HeadSplitPercent)).ToString());
        }

        /// <summary>超时选项变化即时持久化（对应老项目is-timeout/timeout键）</summary>
        public void SaveTimeoutSetting()
        {
            var iniPath = Configs.AppPath + "\\" + Configs.SystemIniPath;
            IniParserUtils.SaveIniData(iniPath, Configs.IniSectionTool, Configs.IniKeyToolIsTimeout, Convert.ToString(IsTimeoutEnabled));
            IniParserUtils.SaveIniData(iniPath, Configs.IniSectionTool, Configs.IniKeyToolTimeout, TimeoutText ?? string.Empty);
        }

        /// <summary>记录请求配置变化即时持久化（对应老项目save-http-request键）</summary>
        public void SaveSaveHttpRequestSetting()
        {
            IniParserUtils.SaveIniData(Configs.AppPath + "\\" + Configs.SystemIniPath, Configs.IniSectionTool, Configs.IniKeySaveHttpRequest, Convert.ToString(SaveHttpRequest));
        }

        private void OnHttpTypeChanged()
        {
            HasBody = HttpUtils.HasRequestBody(HttpType);
        }

        #endregion

        #region 参数行管理

        private void OnAddParam(object obj)
        {
            AddFormParam();
        }

        private void AddFormParam()
        {
            FormParams.Add(new HttpFormParamItem { Key = string.Empty, ParamType = "text", Value = string.Empty });
        }

        private void OnDeleteParam(object obj)
        {
            if (obj is HttpFormParamItem item)
            {
                FormParams.Remove(item);
            }
        }

        private void OnClearAllParam(object obj)
        {
            FormParams.Clear();
        }

        /// <summary>file类型参数值双击选择文件（对应老项目ValueTextBox_DoubleClick）</summary>
        private void OnBrowseParamFile(object obj)
        {
            if (obj is HttpFormParamItem item && "file".Equals(item.ParamType))
            {
                var dialog = new Microsoft.Win32.OpenFileDialog { Title = "选择文件" };
                if (dialog.ShowDialog() == true)
                {
                    item.Value = dialog.FileName;
                }
            }
        }

        private void OnAddHeader(object obj)
        {
            AddHeadParam();
        }

        private void AddHeadParam()
        {
            HeadParams.Add(new HttpParamItem { Key = string.Empty, Value = string.Empty });
        }

        private void OnDeleteHeader(object obj)
        {
            if (obj is HttpParamItem item)
            {
                HeadParams.Remove(item);
            }
        }

        private void OnClearAllHeader(object obj)
        {
            HeadParams.Clear();
        }

        private void OnClearResponse(object obj)
        {
            HttpResponse = string.Empty;
        }

        #endregion

        #region 请求发送（对应老项目SendUrlButton_Click）

        private async void OnSendHttpRequest(object obj)
        {
            if ("停止".Equals(SendButtonText))
            {
                // 发起停止（对应老项目_requestState.Request.Abort()）
                try
                {
                    _currentRequest?.Abort();
                }
                catch { }
                return;
            }

            var url = HttpUrl;
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("请求URL不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!url.StartsWith(HttpUtils.Http) && !url.StartsWith(HttpUtils.Https))
            {
                url = HttpUtils.Http + url;
                HttpUrl = url;
            }

            SendButtonText = "停止";
            HttpResponse = string.Empty;
            var httpSend = new HttpSend { Url = url, Type = HttpType };
            var requestParams = new List<HttpSendParam>();

            try
            {
                var timeout = System.Threading.Timeout.Infinite; // 表示不超时
                if (IsTimeoutEnabled && !string.IsNullOrEmpty(TimeoutText) && int.TryParse(TimeoutText, out var t) && t > 0)
                {
                    timeout = t * 1000;
                }

                var headDic = CollectHeadParams(requestParams);
                var formData = CollectFormParams(requestParams);
                httpSend.ParamList = requestParams;

                // 生成拼接参数（用于非文件上传）
                var paramStr = GetParamStr(formData.TextParams);
                var contentType = HttpUtils.ContentTypeJson;
                switch (ParamModeIndex)
                {
                    case 0: // Form
                        if (HasBody)
                        {
                            // 带请求体的方式才会有form的contentType
                            contentType = FormIsFormData ? HttpUtils.ContentTypeFormData : HttpUtils.ContentTypeFormUrlencoded;
                        }
                        else
                        {
                            // 无请求体方式（Get/Head/Options）配置了Form则清除
                            paramStr = "";
                        }
                        break;
                    case 1: // Raw
                        // post和get都会有raw的contenttype
                        if (RawTypeEnum.TEXT.Equals(RawType, StringComparison.OrdinalIgnoreCase))
                        {
                            contentType = HttpUtils.ContentTypeText;
                        }
                        else if (RawTypeEnum.JAVASCRIPT.Equals(RawType, StringComparison.OrdinalIgnoreCase))
                        {
                            contentType = HttpUtils.ContentTypeJavascript;
                        }
                        else if (RawTypeEnum.JSON.Equals(RawType, StringComparison.OrdinalIgnoreCase))
                        {
                            contentType = HttpUtils.ContentTypeJson;
                        }
                        else if (RawTypeEnum.HTML.Equals(RawType, StringComparison.OrdinalIgnoreCase))
                        {
                            contentType = HttpUtils.ContentTypeHtml;
                        }
                        else if (RawTypeEnum.XML.Equals(RawType, StringComparison.OrdinalIgnoreCase))
                        {
                            contentType = HttpUtils.ContentTypeXml;
                        }
                        paramStr = formData.TextParams.ContainsKey("RawBody") ? formData.TextParams["RawBody"].FirstOrDefault() : RawBody;
                        break;
                }
                if (HasBody)
                {
                    // 带请求体的方式才会有Expect100Continue
                    httpSend.Expect100Continue = Expect100Continue;
                }
                httpSend.ContentType = contentType;

                HttpWebRequest request;
                if (ParamModeIndex == 0 && HasBody && FormIsFormData && formData.HasFiles)
                {
                    // 使用 multipart/form-data 上传文件
                    request = HttpUtils.HttpRequestMultipart(url, HttpType, formData.TextParams, formData.Files, timeout, headDic, Expect100Continue);
                }
                else
                {
                    request = HttpUtils.HttpRequest(url, HttpType, paramStr, contentType, timeout, headDic, Expect100Continue);
                }

                _currentRequest = request;
                await Task.Run(() =>
                {
                    try
                    {
                        var response = (HttpWebResponse)request.GetResponse();
                        using var stream = response.GetResponseStream();
                        using var reader = new System.IO.StreamReader(stream, Encoding.UTF8);
                        var content = reader.ReadToEnd();
                        RunOnUi(() => HttpResponse = content);
                        httpSend.Result = content;
                        httpSend.Code = Convert.ToString((int)response.StatusCode);
                    }
                    catch (WebException ex)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("响应异常!");
                        sb.AppendLine($"信息:{(ex.Status == WebExceptionStatus.RequestCanceled ? "手动停止!" : "")}{ex.Message}");
                        sb.AppendLine($"状态:{ex.Status}");
                        RunOnUi(() => HttpResponse = sb.ToString());
                        httpSend.Result = ex.Message;
                        var resp = ex.Response as HttpWebResponse;
                        httpSend.Code = null != resp ? Convert.ToString((int)resp.StatusCode) : "null";
                    }
                });
            }
            catch (BusinessException ex)
            {
                // 属于业务异常,不记录发送记录
                MessageBox.Show(ex.Message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                httpSend = null; // 不保存
            }
            catch (Exception ex)
            {
                HttpResponse = ex.Message + Environment.NewLine;
                if (httpSend != null)
                {
                    httpSend.Result = ex.Message;
                    httpSend.Code = Convert.ToString((int)HttpStatusCode.BadRequest);
                }
            }
            finally
            {
                SendButtonText = "发送";
                _currentRequest = null;
                if (SaveHttpRequest && httpSend != null && httpSend.Url != null)
                {
                    HttpSendService.Instance.Save(httpSend);
                }
            }
        }

        /// <summary>
        /// 手工保存当前配置为一条发送记录（对应老项目SaveHttpButton_Click）
        /// </summary>
        private void OnSaveHttpConfig(object obj)
        {
            var url = HttpUrl;
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("请求URL不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!url.StartsWith(HttpUtils.Http) && !url.StartsWith(HttpUtils.Https))
            {
                url = HttpUtils.Http + url;
                HttpUrl = url;
            }
            HttpResponse = string.Empty;

            var httpSend = new HttpSend { Url = url, Type = HttpType };
            var requestParams = new List<HttpSendParam>();
            var headDic = CollectHeadParams(requestParams);
            var formData = CollectFormParams(requestParams);
            httpSend.ParamList = requestParams;

            var contentType = HttpUtils.ContentTypeJson;
            switch (ParamModeIndex)
            {
                case 0: // Form
                    if (HasBody)
                    {
                        contentType = FormIsFormData ? HttpUtils.ContentTypeFormData : HttpUtils.ContentTypeFormUrlencoded;
                    }
                    break;
                case 1: // Raw
                    if (RawTypeEnum.TEXT.Equals(RawType, StringComparison.OrdinalIgnoreCase))
                    {
                        contentType = HttpUtils.ContentTypeText;
                    }
                    else if (RawTypeEnum.JAVASCRIPT.Equals(RawType, StringComparison.OrdinalIgnoreCase))
                    {
                        contentType = HttpUtils.ContentTypeJavascript;
                    }
                    else if (RawTypeEnum.JSON.Equals(RawType, StringComparison.OrdinalIgnoreCase))
                    {
                        contentType = HttpUtils.ContentTypeJson;
                    }
                    else if (RawTypeEnum.HTML.Equals(RawType, StringComparison.OrdinalIgnoreCase))
                    {
                        contentType = HttpUtils.ContentTypeHtml;
                    }
                    else if (RawTypeEnum.XML.Equals(RawType, StringComparison.OrdinalIgnoreCase))
                    {
                        contentType = HttpUtils.ContentTypeXml;
                    }
                    break;
            }
            if (HasBody)
            {
                httpSend.Expect100Continue = Expect100Continue;
            }
            httpSend.ContentType = contentType;
            httpSend.Result = "手工保存，无结果！";
            httpSend.Code = Convert.ToString((int)HttpStatusCode.OK);
            HttpSendService.Instance.Save(httpSend);
        }

        /// <summary>收集Head参数并返回键值字典（对应老项目GetHeadParam）</summary>
        private Dictionary<string, string> CollectHeadParams(List<HttpSendParam> paramList)
        {
            var dic = new Dictionary<string, string>();
            var line = 1;
            foreach (var item in HeadParams)
            {
                if (!string.IsNullOrEmpty(item.Value) && string.IsNullOrEmpty(item.Key))
                {
                    throw new BusinessException($"第{line}行，参数名不能为空");
                }
                if (string.IsNullOrEmpty(item.Key)) continue;
                dic[item.Key] = item.Value;
                paramList.Add(new HttpSendParam { Type = HttpSendParam.TYPE_HEAD, ParamKey = item.Key, ParamValue = item.Value });
                line++;
            }
            return dic;
        }

        /// <summary>收集Form或Raw参数（对应老项目GetFormParamWithFiles），Raw时放入RawType/RawBody键</summary>
        private FormData CollectFormParams(List<HttpSendParam> paramList)
        {
            var formData = new FormData();
            if (ParamModeIndex == 0)
            {
                var line = 1;
                foreach (var item in FormParams)
                {
                    if (!string.IsNullOrEmpty(item.Value) && string.IsNullOrEmpty(item.Key))
                    {
                        throw new BusinessException($"第{line}行，参数名不能为空");
                    }
                    if (string.IsNullOrEmpty(item.Key))
                    {
                        line++;
                        continue;
                    }
                    paramList.Add(new HttpSendParam { Type = HttpSendParam.TYPE_BODY, ParamKey = item.Key, ParamType = item.ParamType, ParamValue = item.Value });
                    AddTextParam(formData, item.Key, item.Value);
                    if ("file".Equals(item.ParamType))
                    {
                        var formFile = FormFile.FromPath(item.Key, item.Value);
                        if (formFile != null)
                        {
                            formData.Files.Add(formFile);
                        }
                    }
                    line++;
                }
            }
            else
            {
                formData.TextParams["RawType"] = new List<string> { RawType };
                formData.TextParams["RawBody"] = new List<string> { RawBody ?? string.Empty };
                paramList.Add(new HttpSendParam { Type = HttpSendParam.TYPE_BODY, ParamKey = "RawType", ParamValue = RawType });
                paramList.Add(new HttpSendParam { Type = HttpSendParam.TYPE_BODY, ParamKey = "RawBody", ParamValue = RawBody ?? string.Empty });
            }
            return formData;
        }

        private static void AddTextParam(FormData formData, string key, string value)
        {
            if (formData.TextParams.ContainsKey(key))
            {
                formData.TextParams[key].Add(value);
            }
            else
            {
                formData.TextParams[key] = new List<string> { value };
            }
        }

        private static string GetParamStr(Dictionary<string, List<string>> paramDic)
        {
            var sb = new StringBuilder();
            foreach (var entry in paramDic)
            {
                foreach (var str in entry.Value)
                {
                    sb.Append(entry.Key).Append("=").Append(System.Uri.EscapeDataString(str ?? string.Empty)).Append("&");
                }
            }
            return sb.ToString();
        }

        private void RunOnUi(Action action)
        {
            var app = Application.Current;
            if (app?.Dispatcher == null) return;
            if (app.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                app.Dispatcher.BeginInvoke(action);
            }
        }

        #endregion

        #region 发送记录（对应老项目SendHistoryButton_Click / AddPostParam）

        private Views.HttpSendFormWindow _historyWindow;

        private void OnViewHttpHistory(object obj)
        {
            if (_historyWindow == null)
            {
                _historyWindow = new Views.HttpSendFormWindow { Owner = Application.Current.MainWindow };
                _historyWindow.Closed += (s, e) => _historyWindow = null;
                _historyWindow.Show(); // 非模态，对应老项目HttpSendForm.Show()
            }
            else
            {
                _historyWindow.Activate();
            }
        }

        /// <summary>
        /// 历史记录回填（对应老项目AddPostParam）：按contentType选择Form/Raw页，还原Head与body参数
        /// </summary>
        public void LoadFromHistory(HttpSend httpSend)
        {
            HttpUrl = httpSend.Url;
            HttpType = httpSend.Type;
            if (HasBody && httpSend.Expect100Continue != null)
            {
                Expect100Continue = Convert.ToBoolean(httpSend.Expect100Continue);
            }

            var paramList = HttpSendParamService.Instance.GetByHttpSendId(httpSend.Id) ?? new List<HttpSendParam>();
            var bodyParamList = paramList.Where(p => HttpSendParam.TYPE_BODY.Equals(p.Type)).ToList();
            var headParamList = paramList.Where(p => HttpSendParam.TYPE_HEAD.Equals(p.Type)).ToList();

            // Head参数回填
            HeadParams.Clear();
            if (headParamList.Count > 0)
            {
                foreach (var param in headParamList)
                {
                    HeadParams.Add(new HttpParamItem { Key = param.ParamKey, Value = param.ParamValue });
                }
            }
            else
            {
                AddHeadParam();
            }

            HttpResponse = string.Empty;
            var isForm = HttpUtils.ContentTypeFormData.Equals(httpSend.ContentType)
                      || HttpUtils.ContentTypeFormUrlencoded.Equals(httpSend.ContentType);
            ParamModeIndex = isForm ? 0 : 1;
            if (isForm)
            {
                FormIsFormData = HttpUtils.ContentTypeFormData.Equals(httpSend.ContentType);
                // 先清掉所有参数，再回填
                FormParams.Clear();
                if (bodyParamList.Count > 0)
                {
                    foreach (var param in bodyParamList)
                    {
                        FormParams.Add(new HttpFormParamItem { Key = param.ParamKey, ParamType = param.ParamType ?? "text", Value = param.ParamValue });
                    }
                }
                else
                {
                    AddFormParam();
                }
            }
            else
            {
                foreach (var param in paramList)
                {
                    if ("RawType".Equals(param.ParamKey))
                    {
                        RawType = param.ParamValue;
                    }
                    else if ("RawBody".Equals(param.ParamKey))
                    {
                        RawBody = param.ParamValue;
                    }
                }
            }
        }

        #endregion

        /// <summary>表单参数数据（包含文本和文件，对应老项目FormData内部类）</summary>
        private class FormData
        {
            public Dictionary<string, List<string>> TextParams { get; } = new Dictionary<string, List<string>>();
            public List<FormFile> Files { get; } = new List<FormFile>();
            public bool HasFiles => Files.Count > 0;
        }
    }

    /// <summary>参数行（key/type/value，type为text或file）</summary>
    [AddINotifyPropertyChangedInterface]
    public class HttpFormParamItem
    {
        public string Key { get; set; }
        public string ParamType { get; set; } = "text";
        public string Value { get; set; }
    }

    /// <summary>参数行（key/value，Head参数用）</summary>
    [AddINotifyPropertyChangedInterface]
    public class HttpParamItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
