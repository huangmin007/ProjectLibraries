using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// XElement Extensions
    /// </summary>
    public static class XElementExtensions
    {
        /// <summary>
        /// 分析 XML 文档，处理模板与引用模板元素
        /// </summary>
        /// <param name="rootElement"> XML 元素或根元素</param>
        /// <param name="templateXName">模板的元素的名称</param>
        /// <param name="refTemplateXName">引用模板的元素名称</param>
        /// <param name="removeRefTemplates">是否移除引用模板元素，如果保留，则会重命名元素名称</param>
        public static void ReplaceTemplateElements(this XElement rootElement, string templateXName, string refTemplateXName, bool removeRefTemplates = true)
        {
            if (rootElement == null || String.IsNullOrWhiteSpace(templateXName) || String.IsNullOrWhiteSpace(refTemplateXName)) return;

            //获取有效的模板元素集合
            var templates = from template in rootElement.Elements(templateXName)
                            let tempName = template.Attribute("Name")?.Value
                            where !String.IsNullOrWhiteSpace(tempName)
                            where !(from refTemplate in template.Descendants(refTemplateXName)
                                    where refTemplate.Attribute("Name")?.Value == tempName
                                    select true).Any()
                            select template;
            if (templates?.Count() <= 0) return;

            //获取引用模板元素集合
            var refTemplates = rootElement.Descendants(refTemplateXName);
            if (refTemplates?.Count() <= 0) return;

            //Analyse Replace
            for (int i = 0; i < refTemplates?.Count(); i++)
            {
                var refTemplate = refTemplates.ElementAt(i);
                var refTempName = refTemplate.Attribute("Name")?.Value;
                if (String.IsNullOrWhiteSpace(refTempName)) continue;

                //在模板集合中查找指定名称的模板
                var temps = from template in templates
                            where refTempName == template.Attribute("Name")?.Value
                            select template;
                if (temps?.Count() <= 0) continue;

                //拷贝模板并更新属性值
                string templateString = temps.First().ToString();
                IEnumerable<XAttribute> attributes = refTemplate.Attributes();
                foreach (var attribute in attributes)
                {
                    if (attribute.Name != "Name")
                        templateString = templateString.Replace($"{{{attribute.Name}}}", attribute.Value);
                }

                refTemplate.AddAfterSelf(XElement.Parse(templateString).Elements());

                if (removeRefTemplates) refTemplate.Remove();
                else refTemplate.Name = $"{refTemplate.Name.LocalName}.Handle";

                i--;
            }
        }

        /// <summary>
        /// 分析 XML 文档，处理模板与引用模板元素
        /// </summary>
        /// <param name="rootElement"> XML 元素或根元素</param>
        /// <param name="templateXName">模板的元素的名称</param>
        /// <param name="refTemplateXName">引用模板的元素名称</param>
        /// <param name="removeRefTemplates">是否移除引用模板元素，如果保留，则会重命名元素名称</param>
        public static void ReplaceTemplateElements(this XElement rootElement, XName templateXName, XName refTemplateXName, bool removeRefTemplates = true) 
            => ReplaceTemplateElements(rootElement, templateXName, refTemplateXName, removeRefTemplates);

        /// <summary>
        /// 分析 XML 文档，处理模板与引用模板元素
        /// <para>元素根节点属性应包括 TemplateXName、RefTemplateXName、RemoveRefTemplates</para>
        /// </summary>
        /// <param name="rootElement"> XML 元素或根元素</param>
        public static void ReplaceTemplateElements(this XElement rootElement)
        {
            if (rootElement == null) return;
            string templateXName = rootElement.Attribute("TemplateXName")?.Value;
            string refTemplateXName = rootElement.Attribute("RefTemplateXName")?.Value;
            bool removeRefTemplates = true;
            if(rootElement.Attribute("RemoveRefTemplates") != null && bool.TryParse(rootElement.Attribute("RemoveRefTemplates").Value, out bool result))
            {
                removeRefTemplates = result;
            }

            ReplaceTemplateElements(rootElement, templateXName, refTemplateXName, removeRefTemplates);
        }

    }
}
