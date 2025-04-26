using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using SpaceCG.Extensions;

namespace SpaceCG.Net
{
    /// <summary>
    /// 调用消息
    /// </summary>
    public sealed class InvokeMessage2
    {
        internal static readonly string XType = nameof(Type);
        internal static readonly string XParameter = "Parameter";
        internal static readonly Regex NamedRegex = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

        /// <summary>  Comment  </summary>
        public string Comment { get; set; }

        /// <summary>
        /// 调用的对象或实例名称
        /// </summary>
        public string ObjectName { get; private set; }
        /// <summary>
        /// 调用的方法或函数名称
        /// </summary>
        public string MethodName { get; private set; }
        /// <summary>
        /// 对象方法或函数全名
        /// </summary>
        internal string ObjectMethod => $"{ObjectName}.{MethodName}";
        /// <summary>
        /// 调用的方法或函数的参数
        /// </summary>
        public object[] Parameters { get; private set; }        
        /// <summary>
        /// 方法或函数的调用是否以异步的方式执行，默认为 false，在 <see cref="SynchronizationContext.Current"/> 以同步方式执行
        /// <para>需要实例对象的方法支持异步，异步执行不会产生异常</para>
        /// </summary>
        public bool Asynchronous { get; private set; } = false;

        internal InvokeMessage2()
        {
        }

        /// <summary>
        /// 调用远程方法或函数的的消息对象
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public InvokeMessage2(string objectName, string methodName)
        {
            if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentNullException("参数不能为空");
            if (!NamedRegex.IsMatch(objectName) || !NamedRegex.IsMatch(methodName))
                throw new ArgumentException("参数只能包含字母、数字、下划线，且以字母开头");

            this.ObjectName = objectName;
            this.MethodName = methodName;
        }
        /// <summary>
        /// 调用远程方法或函数的的消息对象
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public InvokeMessage2(string objectName, string methodName, params object[] parameters):this(objectName, methodName)
        {
            this.Parameters = parameters;
        }
        /// <summary>
        /// 调用远程方法或函数的的消息对象
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="asynchronous"></param>
        /// <param name="parameters"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public InvokeMessage2(string objectName, string methodName, bool asynchronous, params object[] parameters):this(objectName, methodName, parameters)
        {
            this.Asynchronous = asynchronous;
        }

        /// <summary>
        /// 试图解析 <see cref="XElement"/> 元素为 <see cref="InvokeMessage2"/> 对象
        /// </summary>
        /// <param name="element"></param>
        /// <param name="invokeMessage"></param>
        /// <param name="exceptionMessage"></param>
        /// <returns></returns>
        public static bool TryParse(XElement element, out InvokeMessage2 invokeMessage, out string exceptionMessage)
        {
            invokeMessage = null;
            exceptionMessage = string.Empty;

            if (element == null)
            {
                exceptionMessage = "Invoke Message is null";
                return false;
            }

            if (element.Name.LocalName == "InvokeMessage")
            {
                var objectName = element.Attribute(nameof(ObjectName))?.Value;
                var methodName = element.Attribute(nameof(MethodName))?.Value;

                if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(methodName) ||
                    NamedRegex.IsMatch(objectName) == false || NamedRegex.IsMatch(methodName) == false)
                {
                    exceptionMessage = "Invoke Message format error: property ObjectName or MethodName is null or not match format";
                    return false;
                }
                
                invokeMessage = new InvokeMessage2()
                {
                    ObjectName = objectName,
                    MethodName = methodName,
                    Comment = element.Attribute(nameof(Comment))?.Value,
                    Asynchronous = bool.TryParse(element.Attribute(nameof(Asynchronous))?.Value, out bool asynchronous) ? asynchronous : false,
                };

                var paramElements = element.Elements(XParameter);
                if (paramElements.Count() > 0)
                {
                    invokeMessage.Parameters = new object[paramElements.Count()];
                    for (int i = 0; i < invokeMessage.Parameters.Length; i++)
                    {
                        XElement paramElement = paramElements.ElementAt(i);
                        if (string.IsNullOrWhiteSpace(paramElement?.Value))
                        {
                            invokeMessage.Parameters[i] = null;
                            continue;
                        }

                        try
                        {
                            string typeString = paramElement.Attribute(XType)?.Value;
                            Type paramType = string.IsNullOrWhiteSpace(typeString) ? null : TypeExtensions.GetType(typeString);
                            if (paramType == null)
                            {
                                invokeMessage.Parameters[i] = paramElement.Value;
                                continue;
                            }

                            object value;
                            if (paramType.IsArray) value = paramElement.Value.Split(new char[] { ',' });
                            else value = paramElement.Value;

                            invokeMessage.Parameters[i] = TypeExtensions.ConvertFrom(value, paramType, out object convertValue) ? convertValue : paramElement.Value;
                        }
                        catch (Exception ex)
                        {
                            invokeMessage.Parameters[i] = paramElement.Value;
                            Trace.TraceWarning($"{nameof(InvokeMessage)} TypeConverter.GetType/ConvertFromString Exception: {ex}");
                        }
                    }
                }
                else
                {
                    //其次 @Parameters 属性
                    string parameters = element.Attribute(nameof(Parameters))?.Value;
                    if (!string.IsNullOrWhiteSpace(parameters))
                        invokeMessage.Parameters = StringExtensions.SplitToObjectArray(parameters);
                }

                return true;
            }
            else if (element.Name.LocalName == "InvokeMessages")
            {

            }
            else
            {
                exceptionMessage = "Invoke Message format error: not InvokeMessage or InvokeMessages";
                return false;
            }

            return true;
        }
    }
}
