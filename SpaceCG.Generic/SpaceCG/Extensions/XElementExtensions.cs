using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SpaceCG.Generic;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// XElement Extensions
    /// </summary>
    public static partial class XElementExtensions
    {
        private static readonly LoggerTrace Logger = new LoggerTrace();

        /// <summary>
        /// 分析 XML 元素，处理模板与引用模板元素
        /// </summary>
        /// <param name="rootElement"> XML 元素或根元素</param>
        /// <param name="templateXName">模板的元素的名称</param>
        /// <param name="refTemplateXName">引用模板的元素名称</param>
        /// <param name="removeRefTemplates">是否移除引用模板元素，如果保留，则会重命名元素名称</param>
        /// <exception cref="ArgumentException"></exception>
        public static void ReplaceTemplateElements(this XElement rootElement, string templateXName, string refTemplateXName, bool removeRefTemplates = true)
        {
            if (rootElement == null || string.IsNullOrWhiteSpace(templateXName) || string.IsNullOrWhiteSpace(refTemplateXName) || templateXName == refTemplateXName)
                throw new ArgumentException($"参数错误, 参数不能为空, 或参数 {nameof(templateXName)} 与 {nameof(refTemplateXName)} 不能为相同元素名称");

            string XName = "Name";
            //获取有效的模板元素集合
            var templates = from template in rootElement.Descendants(templateXName)
                            let tempName = template.Attribute(XName)?.Value
                            where !string.IsNullOrWhiteSpace(tempName)
                            where !(from refTemplate in template.Descendants(refTemplateXName)
                                    where refTemplate.Attribute(XName)?.Value == tempName
                                    select true).Any()
                            select template;
            if (templates?.Count() <= 0) return;

            //获取有效的引用模板元素集合
            var refTemplates = rootElement.Descendants(refTemplateXName);
            if (refTemplates?.Count() <= 0) return;

            //Analyse And Replace
            for (int i = 0; i < refTemplates?.Count(); i++)
            {
                var refTemplate = refTemplates.ElementAt(i);
                var refTempName = refTemplate.Attribute(XName)?.Value;
                if (string.IsNullOrWhiteSpace(refTempName)) continue;

                //在模板集合中查找指定名称的模板
                var temps = from template in templates
                            where refTempName == template.Attribute(XName)?.Value
                            select template;
                if (temps?.Count() <= 0) continue;

                //拷贝模板并更新属性值
                string templateString = temps.First().ToString();
                IEnumerable<XAttribute> attributes = refTemplate.Attributes();
                foreach (XAttribute attribute in attributes)
                {
                    if (attribute.Name != XName)
                        templateString = templateString.Replace($"{{{attribute.Name}}}", attribute.Value);
                }

                refTemplate.AddAfterSelf(XElement.Parse(templateString).Elements());

                if (removeRefTemplates) refTemplate.Remove();
                else refTemplate.Name = $"{refTemplate.Name.LocalName}.Handle";

                i--;
            }
        }

        /// <summary>
        /// 分析 XML 元素，处理模板与引用模板元素
        /// </summary>
        /// <param name="rootElement"> XML 元素或根元素</param>
        /// <param name="templateXName">模板的元素的名称</param>
        /// <param name="refTemplateXName">引用模板的元素名称</param>
        /// <param name="removeRefTemplates">是否移除引用模板元素，如果保留，则会重命名元素名称</param>
        /// <exception cref="ArgumentException"></exception>
        public static void ReplaceTemplateElements(this XElement rootElement, XName templateXName, XName refTemplateXName, bool removeRefTemplates = true)
            => ReplaceTemplateElements(rootElement, templateXName?.LocalName, refTemplateXName?.LocalName, removeRefTemplates);

        /// <summary>
        /// 分析 XML 元素，处理模板与引用模板元素
        /// <para>根节点元素属性应包括 TemplateXName, RefTemplateXName, RemoveRefTemplates(可选属性, Boolean 类型值, 默认为 true)</para>
        /// </summary>
        /// <param name="rootElement"> XML 元素或根元素</param>
        /// <exception cref="ArgumentException"></exception>
        public static void ReplaceTemplateElements(this XElement rootElement)
        {
            if(rootElement == null) return;

            const string XTemplateXName = "TemplateXName";
            const string XRefTemplateXName = "RefTemplateXName";
            const string XRemoveRefTemplates = "RemoveRefTemplates";

            string templateXName = string.IsNullOrWhiteSpace(rootElement.Attribute(XTemplateXName)?.Value) ? "Template" : rootElement.Attribute(XTemplateXName).Value;
            string refTemplateXName = string.IsNullOrWhiteSpace(rootElement.Attribute(XRefTemplateXName)?.Value) ? "RefTemplate" : rootElement.Attribute(XRefTemplateXName).Value;
            
            bool removeRefTemplates = true;
            if (rootElement.Attribute(XRemoveRefTemplates) != null && bool.TryParse(rootElement.Attribute(XRemoveRefTemplates).Value, out bool result))
            {
                removeRefTemplates = result;
            }

            ReplaceTemplateElements(rootElement, templateXName, refTemplateXName, removeRefTemplates);
        }

    }
}
