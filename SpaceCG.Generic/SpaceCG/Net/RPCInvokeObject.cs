using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using SpaceCG.Extensions;
using System.Xml.Linq;

namespace SpaceCG.Net
{

    /// <summary> 方法调用的状态 </summary>
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
    /// RPC 消息格式类型
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
        const string XType = "Type";
        const string XParameter = "Parameter";
        const string XInvokeMessage = nameof(InvokeMessage);
        const string XObjectName = nameof(ObjectName);
        const string XMethodName = nameof(MethodName);
        const string XParameters = nameof(Parameters);
        const string XSynchronous = nameof(Synchronous);
        const string XComment = nameof(Comment);

        /// <summary>
        /// 消息的格式类型
        /// </summary>
        /// internal FormatType FormatType { get; private set; } = FormatType.Code;

        /// <summary>
        /// 调用的对象或实例名称
        /// </summary>
        public string ObjectName { get; set; }
        /// <summary>
        /// 调用的方法或函数名称
        /// </summary>
        public string MethodName { get; set; }
        /// <summary>
        /// 对象方法或函数名称
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
        /// 同步或异步调用方法或函数
        /// </summary>
        public bool Synchronous { get; set; } = true;

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
        /// <param name="synchronous"></param>
        /// <param name="comment"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public InvokeMessage(string objectName, string methodName, object[] parameters, bool synchronous = true, string comment = null)
        {
            if (string.IsNullOrWhiteSpace(objectName)) throw new ArgumentNullException(nameof(objectName), "参数不能为空");
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(objectName), "参数不能为空");

            this.Comment = comment;
            this.ObjectName = objectName;
            this.MethodName = methodName;
            this.Parameters = parameters;
            this.Synchronous = synchronous;
        }
        /// <summary>
        /// 调用远程方法或函数的的消息对象
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="ArgumentNullException"></exception>
        internal InvokeMessage(XElement message)
        {
            if (!IsValid(message))
                throw new ArgumentNullException(nameof(message), $"{nameof(XElement)} 数据格式错误，缺少必要属性或值");

            Comment = message.Attribute(XComment)?.Value;
            ObjectName = message.Attribute(XObjectName)?.Value;
            MethodName = message.Attribute(XMethodName)?.Value;

            if (message.Attribute(XSynchronous) != null)
            {
                string syncValue = message.Attribute(XSynchronous).Value;
                if (!string.IsNullOrWhiteSpace(syncValue) && bool.TryParse(syncValue, out bool sync))
                    Synchronous = sync;
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
                string parameters = message.Attribute(XParameters)?.Value;
                if (!string.IsNullOrWhiteSpace(parameters)) Parameters = StringExtensions.SplitToObjectArray(parameters);
            }
        }

        /// <summary>
        /// 检查对象是否有效，是否符合协议要求
        /// </summary>
        /// <returns>符合协议要求，返回 true</returns>
        public bool IsValid() => !string.IsNullOrWhiteSpace(ObjectName) && !string.IsNullOrWhiteSpace(MethodName);
        /// <summary>
        /// 检查对象是否有效，是否符合协议要求
        /// </summary>
        /// <param name="message"></param>
        /// <returns>符合协议要求，返回 true</returns>
        public static bool IsValid(XElement message)
        {
            if (message == null) return false;

            if (message.Name.LocalName != XInvokeMessage) return false;
            if (string.IsNullOrWhiteSpace(message.Attribute(XObjectName)?.Value)) return false;
            if (string.IsNullOrWhiteSpace(message.Attribute(XMethodName)?.Value)) return false;

            return true;
        }

        /// <summary>
        /// 返回表示当前对象 <see cref="XElement"/> 格式的字符串
        /// </summary>
        /// <returns></returns>
        public string ToXMLString()
        {
            const string SPACE = " ";
            StringBuilder builder = new StringBuilder(RPCServer.BUFFER_SIZE);
            string xsynchronous = !Synchronous ? $"{XSynchronous}=\"{Synchronous}\"{SPACE}" : "";
            string xcomment = !string.IsNullOrWhiteSpace(Comment) ? $"{XComment}=\"{Comment}\"{SPACE}" : "";
            builder.AppendLine($"<{XInvokeMessage} {XObjectName}=\"{ObjectName}\" {XMethodName}=\"{MethodName}\" {xsynchronous}{xcomment}>");

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
                        builder.AppendLine($"<{XParameter} {XType}=\"{paramType.FullName}\">{param}</{XParameter}>");
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
                    builder.AppendLine($"<{XParameter} {XType}=\"{paramType.FullName}\">{paramString}</{XParameter}>");
                }
            }
            builder.Append($"</{nameof(InvokeMessage)}>");

            return builder.ToString();
        }
        /// <summary>
        /// 返回表示当前对象 <see cref="XElement"/> 格式的字符串
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ToXMLString(IEnumerable<InvokeMessage> invokeMessages)
        {
            if (invokeMessages?.Count() == 0)
                throw new ArgumentNullException(nameof(invokeMessages), "集合参数不能为空");

            string InvokeMessages = $"{nameof(InvokeMessage)}s";
            StringBuilder builer = new StringBuilder(RPCServer.BUFFER_SIZE);
            builer.AppendLine($"<{InvokeMessages}>");
            for (int i = 0; i < invokeMessages.Count(); i++)
            {
                builer.AppendLine(invokeMessages.ElementAt(i).ToXMLString());
            }
            builer.AppendLine($"</{InvokeMessages}>");

            return builer.ToString();
        }
        /// <summary>
        /// 返回表示当前对象  <see cref="JsonDocument"/>  格式的字符串
        /// </summary>
        /// <returns></returns>
        public string ToJSONString()
        {
            return null;
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
        public static bool IsValid(JsonDocument message)
        {
            return false;
        }        
        /// <summary>
        /// 返回表示当前对象 <see cref="JsonDocument"/> 格式的字符串
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ToJSONString(IEnumerable<InvokeMessage> invokeResults)
        {
            return null;
        }
#endif
        /// <inheritdoc />
        internal string ToString(MessageFormatType formatType)
        {
            return formatType == MessageFormatType.XML ? ToXMLString() : formatType == MessageFormatType.JSON ? ToJSONString() : ToString();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{nameof(InvokeMessage)} {nameof(ObjectName)}=\"{ObjectName}\", {nameof(MethodName)}=\"{MethodName}\"]";
        }
    }


    /// <summary>
    /// 远程方法或函数调用后返回的结果或信息对象
    /// </summary>
    public class InvokeResult
    {
        const string XInvokeResult = nameof(InvokeResult);
        const string XStatusCode = nameof(StatusCode);
        const string XObjectMethod = nameof(ObjectMethod);
        const string XReturnType = nameof(ReturnType);
        const string XReturnValue = nameof(ReturnValue);
        const string XExceptionMessage = nameof(ExceptionMessage);

        /// <summary> 方法的调用状态 </summary>
        public InvokeStatusCode StatusCode { get; internal set; } = InvokeStatusCode.Unknow;

        /// <summary> 对象或实例的方法或函数的完整名称 </summary>
        public string ObjectMethod { get; internal set; }

        /// <summary> 方法的返回类型 </summary>
        public Type ReturnType { get; internal set; }

        /// <summary> 方法的返回值 </summary>
        public object ReturnValue { get; internal set; }

        /// <summary> 方法执行失败 <see cref="StatusCode"/> 小于 0 时的异常信息 </summary>
        public string ExceptionMessage { get; internal set; }

        /// <summary>
        /// 方法调用返回的结果对象
        /// </summary>
        /// <param name="status"></param>
        internal InvokeResult(InvokeStatusCode status)
        {
            this.StatusCode = status;
        }
        /// <summary>
        /// 方法调用返回的结果对象
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
        /// 方法调用返回的结果对象
        /// </summary>
        /// <param name="result"></param>
        /// <exception cref="ArgumentException"></exception>
        internal InvokeResult(XElement result)
        {
            if (!IsValid(result))
                throw new ArgumentException(nameof(result), $"{nameof(XElement)} 数据格式错误");

            ReturnValue = result.Attribute(XReturnValue)?.Value;
            ObjectMethod = result.Attribute(XObjectMethod)?.Value;
            ExceptionMessage = result.Attribute(XExceptionMessage)?.Value;
            StatusCode = Enum.TryParse(result.Attribute(XStatusCode)?.Value, out InvokeStatusCode status) ? status : InvokeStatusCode.Unknow;

            try
            {
                if (result.Attribute(XReturnType) != null)
                    ReturnType = Type.GetType(result.Attribute(XReturnType).Value, false, true);
            }
            catch (Exception ex)
            {
                RPCServer.Logger.Error($"{nameof(InvokeResult)} GetType Exception:: {ex}");
            }

            if (ReturnType != null && ReturnType != typeof(void) && result.Attribute(XReturnValue) != null)
            {
                string value = result.Attribute(XReturnValue).Value;
                ReturnValue = !string.IsNullOrWhiteSpace(value) && TypeExtensions.ConvertFrom(value, ReturnType, out object conversionValue) ? conversionValue : value;
            }
        }

        /// <summary>
        /// 检查对象是否有效，是否符合协议要求
        /// </summary>
        /// <param name="result"></param>
        /// <returns>符合协议要求，返回 true</returns>
        public static bool IsValid(XElement result)
        {
            if (result == null) return false;
            if (result.Name.LocalName != nameof(InvokeResult)) return false;
            if (string.IsNullOrWhiteSpace(result.Attribute(XStatusCode)?.Value)) return false;

            return true;
        }

        /// <summary>
        /// 返回表示当前对象 <see cref="XElement"/> 格式的字符串
        /// </summary>
        /// <returns></returns>
        public string ToXMLString()
        {
            const string SPACE = " ";
            string returnType = "";
            string returnValue = "";
            string objectMethod = !string.IsNullOrWhiteSpace(ObjectMethod) ? $"{XObjectMethod}=\"{ObjectMethod}\"{SPACE}" : "";
            string exceptionMessage = StatusCode < InvokeStatusCode.Success ? $"{XExceptionMessage}=\"{ExceptionMessage}\"{SPACE}" : "";

            if (ReturnType == null || ReturnType == typeof(void))
            {
                returnType = "";
                returnValue = "";
            }
            else if (ReturnType == typeof(string))
            {
                returnType = $"{XReturnType}=\"{ReturnType.FullName}\"{SPACE}";
                returnValue = $"{XReturnValue}=\"{ReturnValue?.ToString() ?? ""}\"{SPACE}";
            }
            else
            {
                returnType = $"{XReturnType}=\"{ReturnType.FullName}\"{SPACE}";
                returnValue = $"{XReturnValue}=\"{ReturnValue?.ToString() ?? ""}\"{SPACE}";

                if (ReturnValue != null)
                {
                    try
                    {
                        TypeConverter converter = TypeDescriptor.GetConverter(ReturnType);
                        if (converter.CanConvertTo(ReturnType))
                            returnValue = $"{XReturnValue}=\"{converter.ConvertToString(ReturnValue)}\"{SPACE}";
                    }
                    catch (Exception ex)
                    {
                        returnValue = $"{XReturnValue}=\"{ReturnValue}\"{SPACE}";
                        RPCServer.Logger.Warn($"{nameof(InvokeResult)} TypeConverter.ConvertToString Exception: {ex}");
                    }
                }
            }

            return $"<{XInvokeResult} {XStatusCode}=\"{(int)StatusCode}\" {objectMethod}{returnType}{returnValue}{exceptionMessage}/>";
        }
        /// <summary>
        /// 返回表示当前对象 <see cref="XElement"/> 格式的字符串
        /// </summary>
        /// <param name="invokeResults"></param>
        /// <param name="formatType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ToString(IEnumerable<InvokeResult> invokeResults, MessageFormatType formatType)
        {
            if (invokeResults?.Count() == 0)
                throw new ArgumentNullException(nameof(invokeResults), "集合参数不能为空");

            StringBuilder builer = new StringBuilder(RPCServer.BUFFER_SIZE);
            if (formatType == MessageFormatType.XML)
            {
                string InvokeResults = $"{nameof(InvokeResult)}s";
                builer.AppendLine($"<{InvokeResults}>");
                for (int i = 0; i < invokeResults.Count(); i++)
                {
                    builer.AppendLine(invokeResults.ElementAt(i).ToXMLString());
                }
                builer.AppendLine($"</{InvokeResults}>");

            }
            else if (formatType == MessageFormatType.JSON)
            {
            }

            return builer.ToString();
        }

        /// <summary>
        /// 返回表示当前对象  <see cref="JsonDocument"/>  格式的字符串
        /// </summary>
        /// <returns></returns>
        public string ToJSONString()
        {
            return null;
        }

#if false
        /// <summary>
        /// 方法调用返回的结果对象
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
        public static bool IsValid(JsonDocument message)
        {
            return false;
        }
        /// <summary>
        /// 返回表示当前对象 <see cref="JsonDocument"/> 格式的字符串
        /// </summary>
        /// <param name="invokeResults"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ToJSONString(IEnumerable<InvokeResult> invokeResults)
        {
            return null;
        }
#endif

        internal string ToString(MessageFormatType formatType)
        {
            return formatType == MessageFormatType.XML ? ToXMLString() : formatType == MessageFormatType.JSON ? ToJSONString() : ToString();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            if (StatusCode == InvokeStatusCode.SuccessAndReturn)
                return $"[{nameof(InvokeResult)} {XStatusCode}=\"{StatusCode}\", {XObjectMethod}=\"{ObjectMethod}\", {XReturnType}=\"{ReturnType}\", {XReturnValue}=\"{ReturnValue}\"]";

            return $"[{nameof(InvokeResult)} {XStatusCode}=\"{StatusCode}\", {XObjectMethod}=\"{ObjectMethod}\"]";
        }
    }

}
