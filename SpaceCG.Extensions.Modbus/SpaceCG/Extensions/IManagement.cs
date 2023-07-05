using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SpaceCG.Generic;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// 对象管理接口
    /// </summary>
    internal interface IManagement
    {
        /// <summary>
        /// 配置管理对象
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="managerName"></param>
        void Configuration(ReflectionController controller, string managerName);

        /// <summary>
        /// 解析 XElement 配置
        /// </summary>
        /// <param name="elements"></param>
        void TryParseElements(IEnumerable<XElement> elements);

        /// <summary>
        /// 在管理对象的配置集合中，跟据事件名称，调用事件
        /// </summary>
        /// <param name="eventName"></param>
        void CallEventName(string eventName);

        /// <summary>
        /// 在管理对象的配置集合中，跟据事件名称，调用事件
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="parentName"></param>
        void CallEventName(string eventName, string parentName);

        /// <summary>
        /// 移除管理对象
        /// </summary>
        void RemoveAll();
    }
}
