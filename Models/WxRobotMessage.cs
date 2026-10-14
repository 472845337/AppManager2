using Newtonsoft.Json;

namespace AppManager2.Models
{
    /// <summary>
    /// 企业微信机器人消息（对应老项目 Pojo/WxRobotMessage）
    /// </summary>
    public class WxRobotMessage
    {
        public const string MsgtypeText = "text";

        /** 消息类型,使用类中的常量 */
        [JsonProperty(propertyName: "msgtype")]
        public string Msgtype { get; set; }

        /** 文本类型数据 */
        [JsonProperty(propertyName: "text")]
        public TextClass Text { get; set; }

        /** 文本类型 */
        public class TextClass
        {
            /** 文本内容 */
            [JsonProperty(propertyName: "content")]
            public string Content { get; set; }
        }
    }
}
