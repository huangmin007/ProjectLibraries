using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using SpaceCG.Extensions;
using SpaceCG.Generic;
using System.Xml.Linq;
using System.Diagnostics;

namespace SpaceCG.Net
{

    /// <summary> 
    /// 方法或函数的调用状态 
    /// </summary>
    public enum InvokeStatusCode
    {
        /// <summary> 未知状态 </summary>
        Unknown = int.MinValue,
        /// <summary> 客户端发送消息，等待服务端响应超时 </summary>
        Timeout = -2,
        /// <summary> 服务端接收到消息数据，但可能调用失败 </summary>
        Failed = -1,
        /// <summary> 服务端接收到消息数据，确定调用成功，方法或函数没有返回参数 </summary>
        Success = 0,
        /// <summary> 服务端接收到消息数据，确定调用成功，方法或函数有返回参数  </summary>
        SuccessAndReturn = 1,
    }

    /// <summary>
    /// 远程方法或函数的调用过程或调用结果的消息对象
    /// </summary>
    public class InvokeResult
    {
        //const string SPACE = " ";
        internal static readonly string XReturn = "Return";

        /// <summary> 远程方法或函数的调用状态 </summary>
        public InvokeStatusCode StatusCode { get; internal set; } = InvokeStatusCode.Unknown;

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
            if (!IsValid(result))
            {
                Trace.TraceWarning($"无效的消息内容：{result}");
                return;
            }

            ObjectMethod = result.Attribute(nameof(ObjectMethod))?.Value;
            ExceptionMessage = result.Attribute(nameof(ExceptionMessage))?.Value;
            StatusCode = Enum.TryParse(result.Attribute(nameof(StatusCode))?.Value, out InvokeStatusCode status) ? status : InvokeStatusCode.Unknown;

            if (StatusCode != InvokeStatusCode.SuccessAndReturn) return;

            string returnTypeString, returnValueString;
            IEnumerable<XElement> returnResults = result.Elements(XReturn);
            if (returnResults?.Count() > 0)
            {
                returnTypeString = returnResults.First().Attribute(nameof(Type))?.Value;
                returnValueString = returnResults.First().Value;
            }
            else if (result.Attribute(nameof(ReturnType)) != null)
            {
                returnTypeString = result.Attribute(nameof(ReturnType)).Value;
                returnValueString = result.Attribute(nameof(ReturnValue)).Value;
            }
            else
            {
                returnTypeString = null;
                returnValueString = null;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(returnTypeString))
                    ReturnType = TypeExtensions.GetType(returnTypeString);

                if (ReturnType != null && ReturnType != typeof(void))
                    ReturnValue = !string.IsNullOrWhiteSpace(returnValueString) && TypeExtensions.ConvertFrom(returnValueString, ReturnType, out object conversionValue) ? conversionValue : returnValueString;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"数据类型转换失败: {ex}");
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


        /// <summary>
        /// 返回表示当前对象 <see cref="XElement"/> 格式的字符串
        /// </summary>
        /// <returns></returns>
        public string ToXMLString()
        {
            StringBuilder builder = new StringBuilder(RPCServer.BUFFER_SIZE);
            builder.Append($"<{nameof(InvokeResult)} {nameof(StatusCode)}=\"{(int)StatusCode}\" ");

            if (!string.IsNullOrWhiteSpace(ObjectMethod)) builder.Append($"{nameof(ObjectMethod)}=\"{ObjectMethod}\" ");
            if (StatusCode < InvokeStatusCode.Success) builder.Append($"{nameof(ExceptionMessage)}=\"{ExceptionMessage}\" ");

            if (StatusCode != InvokeStatusCode.SuccessAndReturn)
            {
                builder.Append("/>");
                return builder.ToString();
            }

            string returnTypeString = ReturnType.FullName;
            string returnValueString = ReturnValue?.ToString();
            if (!string.IsNullOrWhiteSpace(returnValueString))
            {
                try
                {
                    TypeConverter converter = TypeDescriptor.GetConverter(ReturnType);
                    if (converter.CanConvertTo(ReturnType)) returnValueString = converter.ConvertToString(ReturnValue);
                }
                catch (Exception ex)
                {
                    returnValueString = ReturnValue.ToString();
                    Trace.TraceWarning($"{nameof(InvokeResult)} TypeConverter.ConvertToString Exception: {ex}");
                }
            }

            builder.AppendLine(">");
            builder.AppendLine($"\t<{XReturn} {nameof(Type)}=\"{returnTypeString}\"><![CDATA[{returnValueString}]]></{XReturn}>");
            builder.Append($"</{nameof(InvokeResult)}>");

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
