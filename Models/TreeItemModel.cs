using AppManager2.Config;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;

namespace AppManager2.Models
{
    /// <summary>
    /// 应用树节点（对应老项目TreeView中的节点，含组节点和应用节点）
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public class TreeItemModel
    {
        public string Name { get; set; }
        public string Flag { get; set; }  // "group" or "child"
        public string Section { get; set; }
        public string ImageKey { get; set; }
        public string GroupName { get; set; }
        public short RunStat { get; set; }
        public bool IsRunning { get; set; }
        public bool IsWarn { get; set; }
        public string Remark { get; set; }

        /// <summary>组节点展开状态（与TreeViewItem双向绑定，VM可在新增项目时定向展开目标组）</summary>
        public bool IsExpanded { get; set; } = true;

        public ObservableCollection<TreeItemModel> Children { get; set; }
        public ICommand ContextMenuCommand { get; set; }

        /// <summary>节点图标（java/bat/exe提取图标/组）</summary>
        public ImageSource Icon { get; set; }

        /// <summary>节点背景色（运行状态联动，组节点为聚合色，由VM维护）</summary>
        public Brush BackgroundBrush { get; set; } = Brushes.White;

        /// <summary>运行状态描述</summary>
        public string RunStatText => Flag == Configs.NodeFlagChild
            ? RunStat switch
            {
                Configs.AppRunStatFail => "启动失败",
                Configs.AppRunStatRunning => "启动中...",
                Configs.AppRunStatSuccess => "运行中",
                Configs.AppRunStatStopping => "停止中...",
                _ => ""
            }
            : "";

        /// <summary>可启动：未运行或启动失败（对应老项目FreshAppColor的startEnabled）</summary>
        public bool CanStart => RunStat == Configs.AppRunStatUnrun || RunStat == Configs.AppRunStatFail;

        /// <summary>可停止：运行成功（对应老项目FreshAppColor的stopEnabled）</summary>
        public bool CanStop => RunStat == Configs.AppRunStatSuccess;

        // 节点右键菜单命令（构建树时由 ViewModel 注入）
        public ICommand DetailCommand { get; set; }
        public ICommand WarnSwitchCommand { get; set; }
        public ICommand WarnOnCommand { get; set; }
        public ICommand WarnOffCommand { get; set; }
        public ICommand StartCommand { get; set; }
        public ICommand StopCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand LocateFileCommand { get; set; }
        public ICommand FreshCommand { get; set; }

        public TreeItemModel()
        {
            Children = new ObservableCollection<TreeItemModel>();
            RunStat = 0;
            IsRunning = false;
            IsWarn = false;
        }
    }
}
