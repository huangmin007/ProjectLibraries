using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using SpaceCG.Extensions;
using System.Xml.Linq;
using System.Threading;

namespace SpaceCG.Net
{

    /// <summary> 
    /// 方法或函数的调用状态 
    /// </summary>
    public enum InvokeStatusCode
    {
        /// <summary> 未知状态 </summary>
        Unknow = -2,
        /// <summary> 调用失败 </summary>
        Failed = -1,
        /// <summary> 调用成功，方法或函数返回参数 </summary>
        Success = 0,
        /// <summary> 调用成功，方法或函数有返回参数  </summary>
        SuccessAndReturn = 1,
    }

    /// <summary>
    /// RPC 协议支持的消息格式类型
    /// </summary>
    public enum MessageFormatType
    {
        /// <summary> Code </summary>
        Code = 0,
        /// <summary> XML 格式 </summary>
        XML = 1,
        /// <summary> JSON 格式 </summary>
        JSON = 2,
    }

    /// <summary>
    /// 调用远程方法或函数的的消息对象
    /// </summary>
    public class InvokeMessage
    {
        const string SPACE = " ";
        const string XType = "Type";
        const string XParameter = "Parameter";

        /// <summary>
        /// 调用的对象或实例名称
        /// </summary>
        public string ObjectName { get; set; }
        /// <summary>
        /// 调用的方法或函数名称
        /// </summary>
        public string MethodName { get; set; }
        /// <summary>
        /// 对象方法或函数全名
        /// </summary>
        internal string ObjectMethod => $"{ObjectName}.{MethodName}";
        /// <summary>
        /// 调用的方法或函数的参数
        /// </summary>
        public object[] Parameters { get; set; }
        /// <summary>
        /// Comment
        /// </summary>
        public string Comment { get; set; }
        /// <summary>
        /// 方法或函数的调用是否以异步的方式执行，默认为 false，在 <see cref="SynchronizationContext.Current"/> 以同步方式执行
        /// <para>需要实例对象的方法支持异步，异步执行不会产生异常</para>
        /// </summary>
        public bool Asynchronous { get; set; } = false;

        /// <summary>
        /// 调用远程方法或函数的的消息对象
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public InvokeMessage(string objectName, string methodName)
        {
            if (string.IsNullOrWhiteSpace(objectName)) throw new ArgumentNullException(nameof(objectName), "参数不能为空");
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(objectName), "参数不能为空");

            this.ObjectName = objectName;
            this.MethodName = methodName;
        }
        /// <summary>
        /// 调用远程方法或函数的的消息对象
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        public InvokeMessage(string objectName, string methodName, object[] parameters) : this(objectName, methodName)
        {
            this.Parameters = parameters;
        }
        /// <summary>
        /// 调用远程方法或函数的的消息对象
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="asynchronous"></param>
        /// <param name="comment"></param>
        public InvokeMessage(string objectName, string methodName, object[] parameters, bool asynchronous, string comment = null) : this(objectName, methodName, parameters)
        {
            this.Comment = comment;
            this.Asynchronous = asynchronous;
        }
        /// <summary>
        /// 调用远程方法或函数的的消息对象
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="ArgumentNullException"></exception>
        internal InvokeMessage(XElement message)
        {
            if (!IsValid(message))
                throw new ArgumentNullException(nameof(message), $"{nameof(XElement)} 调用消息不符合协议要求");

            Comment = message.Attribute(nameof(Comment))?.Value;
            ObjectName = message.Attribute(nameof(ObjectName))?.Value;
            MethodName = message.Attribute(nameof(MethodName))?.Value;

            if (message.Attribute(nameof(Asynchronous)) != null)
            {
                string asyncStringValue = message.Attribute(nameof(Asynchronous)).Value;
                if (!string.IsNullOrWhiteSpace(asyncStringValue) && bool.TryParse(asyncStringValue, out bool asyncBooleanValue))
                    Asynchronous = asyncBooleanValue;
            }

            //优先 Parameter 节点
            var paramElements = message.Elements(XParameter);
            if (paramElements.Count() > 0)
            {
                Parameters = new object[paramElements.Count()];
                for (int i = 0; i < Parameters.Length; i++)
                {
                    XElement paramElement = paramElements.ElementAt(i);
                    if (string.IsNullOrWhiteSpace(paramElement?.Value))
                    {
                        Parameters[i] = null;
                        continue;
                    }

                    try
                    {
                        string typeString = paramElement.Attribute(XType)?.Value;
                        Type paramType = string.IsNullOrWhiteSpace(typeString) ? null : Type.GetType(typeString, false, true);
                        if (paramType == null)
                        {
                            Parameters[i] = paramElement.Value;
                            continue;
                        }

                        object value;
                        if (paramType.IsArray) value = paramElement.Value.Split(new char[] { ',' }); //, StringSplitOptions.RemoveEmptyEntries);
                        else value = paramElement.Value;

                        Parameters[i] = TypeExtensions.ConvertFrom(value, paramType, out object convertValue) ? convertValue : paramElement.Value;
                    }
                    catch (Exception ex)
                    {
                        Parameters[i] = paramElement.Value;
                        RPCServer.Logger.Warn($"{nameof(InvokeMessage)} TypeConverter.GetType/ConvertFromString Exception: {ex}");
                    }
                }
            }
            else
            {
                //其次 @Parameters 属性
                string parameters = message.Attribute(nameof(Parameters))?.Value;
                if (!string.IsNullOrWhiteSpace(parameters)) Parameters = StringExtensions.SplitToObjectArray(parameters);
            }
        }

        /// <summary>
        /// 检查消息是否有效，是否符合协议要求
        /// </summary>
        /// <returns>符合协议要求，返回 true</returns>
        internal bool IsValid() => !string.IsNullOrWhiteSpace(ObjectName) && !string.IsNullOrWhiteSpace(MethodName);
        /// <summary>
        /// 检查消息是否有效，是否符合协议要求
        /// </summary>
        /// <param name="message"></param>
        /// <returns>符合协议要求，返回 true</returns>
        internal static bool IsValid(XElement message)
        {
            if (message == null) return false;
            if (message.Name.LocalName == nameof(InvokeMessage))
            {
                return !string.IsNullOrWhiteSpace(message.Attribute(nameof(ObjectName))?.Value)
                    && !string.IsNullOrWhiteSpace(message.Attribute(nameof(MethodName))?.Value);
            }
            else if (message.Name.LocalName == $"{nameof(InvokeMessage)}s")
            {
                var elements = message.Elements(nameof(InvokeMessage));
                if (elements.Count() <= 0) return false;
                foreach (var element in elements)
                    if (!IsValid(element)) return false;
                return true;
            }
            return false;
        }

#if false
        /// <summary>
        /// 调用远程方法或函数的的消息对象
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="ArgumentNullException"></exception>
        internal InvokeMessage(JsonDocument message)
        {            
        }
        /// <summary>
        /// 检查对象是否有效，是否符合协议要求
        /// </summary>
        /// <param name="message"></param>
        /// <returns>符合协议要求，返回 true</returns>
        internal static bool IsValid(JsonDocument message)
        {
            return false;
        }
#endif

        /// <summary>
        /// 返回表示当前对象 <see cref="XElement"/> 格式的字符串
        /// </summary>
        /// <returns></returns>
        private string ToXMLString()
        {
            StringBuilder builder = new StringBuilder(RPCServer.BUFFER_SIZE);
            string xasynchronous = Asynchronous ? $"{nameof(Asynchronous)}=\"{Asynchronous}\"{SPACE}" : "";
            string xcomment = !string.IsNullOrWhiteSpace(Comment) ? $"{nameof(Comment)}=\"{Comment}\"{SPACE}" : "";
            builder.AppendLine($"<{nameof(InvokeMessage)} {nameof(ObjectName)}=\"{ObjectName}\" {nameof(MethodName)}=\"{MethodName}\" {xasynchronous}{xcomment}>");

            if (Parameters?.Length > 0)
            {
                Type stringType = typeof(string);
                foreach (var param in Parameters)
                {
                    builder.Append("\t");
                    if (param == null)
                    {
                        builder.AppendLine($"<{XParameter} />");
                        continue;
                    }

                    Type paramType = param.GetType();
                    if (paramType == stringType)
                    {
                        builder.AppendLine($"<{XParameter} {XType}=\"{paramType.FullName}\"><![CDATA[{param}]]></{XParameter}>");
                        continue;
                    }

                    string paramString = param.ToString();
                    try
                    {
                        TypeConverter converter = TypeDescriptor.GetConverter(paramType);
                        if (converter.CanConvertTo(paramType)) paramString = converter.ConvertToString(param);
                    }
                    catch (Exception ex)
                    {
                        paramString = param.ToString();
                        RPCServer.Logger.Warn($"{nameof(InvokeMessage)} TypeConverter.ConvertToString Exception: {ex}");
                    }
                    builder.AppendLine($"<{XParameter} {XType}=\"{paramType.FullName}\"><![CDATA[{paramString}]]></{XParameter}>");
                }
            }
            builder.Append($"</{nameof(InvokeMessage)}>");

            return builder.ToString();
        }
        /// <summary>
        /// 返回表示当前对象  <see cref="JsonDocument"/>  格式的字符串
        /// </summary>
        /// <returns></returns>
        private string ToJSONString()
        {
            return null;
        }
        /// <inheritdoc />
        internal string ToFormatString(MessageFormatType formatType)
        {
            return formatType == MessageFormatType.XML ? ToXMLString() : formatType == MessageFormatType.JSON ? ToJSONString() : ToString();
        }

        /// <summary>
        /// 返回表示对象集合的指定的格式字符串
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <param name="formatType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static string ToFormatString(IEnumerable<InvokeMessage> invokeMessages, MessageFormatType formatType)
        {
            if (invokeMessages == null)
                throw new ArgumentNullException(nameof(invokeMessages), "集合参数不能为空");
            StringBuilder builer = new StringBuilder(RPCServer.BUFFER_SIZE);

            if (formatType == MessageFormatType.XML)
            {
                string InvokeMessages = $"{nameof(InvokeMessage)}s";
                builer.AppendLine($"<{InvokeMessages}>");
                for (int i = 0; i < invokeMessages.Count(); i++)
                {
                    builer.AppendLine(invokeMessages.ElementAt(i).ToXMLString());
                }
                builer.AppendLine($"</{InvokeMessages}>");
            }
            else if (formatType == MessageFormatType.JSON)
            {

            }
            else
            {

            }

            return builer.ToString();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (Asynchronous)
                return $"[{nameof(InvokeMessage)} {nameof(ObjectName)}=\"{ObjectName}\", {nameof(MethodName)}=\"{MethodName}\", {nameof(Asynchronous)}=\"{Asynchronous}\"]";
            else
                return $"[{nameof(InvokeMessage)} {nameof(ObjectName)}=\"{ObjectName}\", {nameof(MethodName)}=\"{MethodName}\"]";
        }
    }


    /// <summary>
    /// 远程方法或函数的调用过程或调用结果的消息对象
    /// </summary>
    public class InvokeResult
    {
        const string SPACE = " ";

        /// <summary> 远程方法或函数的调用状态 </summary>
        public InvokeStatusCode StatusCode { get; internal set; } = InvokeStatusCode.Unknow;

        /// <summary> 对象实例的方法的完整名称 </summary>
        public string ObjectMethod { get; internal set; }

        /// <summary> 远程方法或函数的返回值类型 </summary>
        public Type ReturnType { get; internal set; }

        /// <summary> 远程方法或函数的返回值 </summary>
        public object ReturnValue { get; internal set; }

        /// <summary> 远程方法或函数调用失败的原因或是异常信息，一般在 <see cref="StatusCode"/> 值小于 0 时该值不为 null  </summary>
        public string ExceptionMessage { get; internal set; }

        /// <summary>
        /// 远程方法或函数的调用过程或调用结果的消息对象
        /// </summary>
        /// <param name="status"></param>
        internal InvokeResult(InvokeStatusCode status)
        {
            this.StatusCode = status;
        }
        /// <summary>
        /// 远程方法或函数的调用过程或调用结果的消息对象
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="objectMethod"></param>
        /// <param name="exceptionMessage"></param>
        internal InvokeResult(InvokeStatusCode statusCode, string objectMethod, string exceptionMessage)
        {
            this.StatusCode = statusCode;
            this.ObjectMethod = objectMethod;
            this.ExceptionMessage = exceptionMessage;
        }
        
        /// <summary>
        /// 远程方法或函数的调用过程或调用结果的消息对象
        /// </summary>
        /// <param name="result"></param>
        /// <exception cref="ArgumentException"></exception>
        internal InvokeResult(XElement result)
        {
            if (!IsValid(result)) return;

            ReturnValue = result.Attribute(nameof(ReturnValue))?.Value;
            ObjectMethod = result.Attribute(nameof(ObjectMethod))?.Value;
            ExceptionMessage = result.Attribute(nameof(ExceptionMessage))?.Value;
            StatusCode = Enum.TryParse(result.Attribute(nameof(StatusCode))?.Value, out InvokeStatusCode status) ? status : InvokeStatusCode.Unknow;

            try
            {
                if (result.Attribute(nameof(ReturnType)) != null)
                    ReturnType = Type.GetType(result.Attribute(nameof(ReturnType)).Value, false, true);
            }
            catch (Exception ex)
            {
                RPCServer.Logger.Error($"{nameof(InvokeResult)} GetType Exception:: {ex}");
            }

            if (ReturnType != null && ReturnType != typeof(void) && result.Attribute(nameof(ReturnValue)) != null)
            {
                string value = result.Attribute(nameof(ReturnValue)).Value;
                ReturnValue = !string.IsNullOrWhiteSpace(value) && TypeExtensions.ConvertFrom(value, ReturnType, out object conversionValue) ? conversionValue : value;
            }
        }
        /// <summary>
        /// 检查消息是否有效，是否符合协议要求
        /// </summary>
        /// <param name="result"></param>
        /// <returns>符合协议要求，返回 true</returns>
        internal static bool IsValid(XElement result)
        {
            if (result == null) return false;
            if (result.Name.LocalName == nameof(InvokeResult))
            {
                return !string.IsNullOrWhiteSpace(result.Attribute(nameof(StatusCode))?.Value);
            }
            else if (result.Name.LocalName == $"{nameof(InvokeResult)}s")
            {
                var elements = result.Elements(nameof(InvokeResult));
                if (elements.Count() <= 0) return false;
                foreach (var element in elements) 
                    if (!IsValid(element)) return false;
                return true;
            }
            return false;
        }

#if false
        /// <summary>
        /// 远程方法或函数的调用过程或调用结果的消息对象
        /// </summary>
        /// <param name="result"></param>
        internal InvokeResult(JsonDocument result)
        {
        }
        /// <summary>
        /// 检查对象是否有效，是否符合协议要求
        /// </summary>
        /// <param name="message"></param>
        /// <returns>符合协议要求，返回 true</returns>
        internal static bool IsValid(JsonDocument message)
        {
            return false;
        }        
#endif

        /// <summary>
        /// 返回表示当前对象 <see cref="XElement"/> 格式的字符串
        /// </summary>
        /// <returns></returns>
        private string ToXMLString()
        {
            string returnType = "";
            string returnValue = "";
            string objectMethod = !string.IsNullOrWhiteSpace(ObjectMethod) ? $"{nameof(ObjectMethod)}=\"{ObjectMethod}\"{SPACE}" : "";
            string exceptionMessage = StatusCode < InvokeStatusCode.Success ? $"{nameof(ExceptionMessage)}=\"{ExceptionMessage}\"{SPACE}" : "";

            if (ReturnType == null || ReturnType == typeof(void))
            {
                returnType = "";
                returnValue = "";
            }
            else if (ReturnType == typeof(string))
            {
                returnType = $"{nameof(ReturnType)}=\"{ReturnType.FullName}\"{SPACE}";
                returnValue = $"{nameof(ReturnValue)}=\"{ReturnValue?.ToString() ?? ""}\"{SPACE}";
            }
            else
            {
                returnType = $"{nameof(ReturnType)}=\"{ReturnType.FullName}\"{SPACE}";
                returnValue = $"{nameof(ReturnValue)}=\"{ReturnValue?.ToString() ?? ""}\"{SPACE}";

                if (ReturnValue != null)
                {
                    try
                    {
                        TypeConverter converter = TypeDescriptor.GetConverter(ReturnType);
                        if (converter.CanConvertTo(ReturnType))
                            returnValue = $"{nameof(ReturnValue)}=\"{converter.ConvertToString(ReturnValue)}\"{SPACE}";
                    }
                    catch (Exception ex)
                    {
                        returnValue = $"{nameof(ReturnValue)}=\"{ReturnValue}\"{SPACE}";
                        RPCServer.Logger.Warn($"{nameof(InvokeResult)} TypeConverter.ConvertToString Exception: {ex}");
                    }
                }
            }

            return $"<{nameof(InvokeResult)} {nameof(StatusCode)}=\"{(int)StatusCode}\" {objectMethod}{returnType}{returnValue}{exceptionMessage}/>";
        }
        /// <summary>
        /// 返回表示当前对象  <see cref="JsonDocument"/>  格式的字符串
        /// </summary>
        /// <returns></returns>
        private string ToJSONString()
        {
            return null;
        }
        /// <summary>
        /// 返回表示当前对象指定的格式字符串
        /// </summary>
        /// <param name="formatType"></param>
        /// <returns></returns>
        internal string ToFormatString(MessageFormatType formatType)
        {
            return formatType == MessageFormatType.XML ? ToXMLString() : formatType == MessageFormatType.JSON ? ToJSONString() : ToString();
        }

        /// <summary>
        /// 返回表示对象集合的指定的格式字符串
        /// </summary>
        /// <param name="invokeResults"></param>
        /// <param name="formatType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static string ToFormatString(IEnumerable<InvokeResult> invokeResults, MessageFormatType formatType)
        {
            if (invokeResults == null)
                throw new ArgumentNullException(nameof(invokeResults), "参数不能为空");

            StringBuilder builder = new StringBuilder(RPCServer.BUFFER_SIZE);
            if (formatType == MessageFormatType.XML)
            {
                string InvokeResults = $"{nameof(InvokeResult)}s";
                builder.AppendLine($"<{InvokeResults}>");
                for (int i = 0; i < invokeResults.Count(); i++)
                {
                    builder.Append("\t");
                    builder.AppendLine(invokeResults.ElementAt(i).ToXMLString());
                }
                builder.AppendLine($"</{InvokeResults}>");
            }
            else if (formatType == MessageFormatType.JSON)
            {

            }
            else
            {
                
            }

            return builder.ToString();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (StatusCode == InvokeStatusCode.Success)
                return $"[{nameof(InvokeResult)} {nameof(StatusCode)}=\"{StatusCode}\", {nameof(ObjectMethod)}=\"{ObjectMethod}\"]";
            if (StatusCode == InvokeStatusCode.SuccessAndReturn)
                return $"[{nameof(InvokeResult)} {nameof(StatusCode)}=\"{StatusCode}\", {nameof(ObjectMethod)}=\"{ObjectMethod}\", {nameof(ReturnType)}=\"{ReturnType}\", {nameof(ReturnValue)}=\"{ReturnValue}\"]";
            else
                return $"[{nameof(InvokeResult)} {nameof(StatusCode)}=\"{StatusCode}\", {nameof(ObjectMethod)}=\"{ObjectMethod}\", {nameof(ExceptionMessage)}=\"{ExceptionMessage}\"]";
        }
    }

}
