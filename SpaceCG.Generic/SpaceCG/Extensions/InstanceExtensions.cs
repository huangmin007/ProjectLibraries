using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// 实例功能扩展库
    /// </summary>
    public static partial class InstanceExtensions
    {
        /// <summary>
        /// log4net.Logger 对象
        /// </summary>
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(nameof(InstanceExtensions));

        /// <summary>
        /// 动态的获取实例的字段对象。注意：包括私有对象
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static object GetInstanceFieldObject(object instanceObj, String fieldName)
        {
            if (instanceObj == null || String.IsNullOrWhiteSpace(fieldName))
                throw new Exception("参数不能为空");

            try
            {
                Type type = instanceObj.GetType();
                FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (fieldInfo == null)
                {
                    Logger.Warn($"在实例对象 {instanceObj} 中，未找到指定字段 {fieldName} 对象");
                    return null;
                }

                return fieldInfo.GetValue(instanceObj);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return null;
        }

        /// <summary>
        /// 动态的设置实例对象的多个属性值，跟据 XML 配置文件节点名称(实例字段对象)及节点属性(字段对象的属性)来个修改实例属性
        /// <para>例如：&lt;Window Left="100" Width="100"/&gt; </para>
        /// <para> Window 是 instanceObj 中的一个实例对象(或变量对象，非静态对象)，其 Left、Width 为 Window 实例对象的原有属性 </para>
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="element"></param>
        public static void SetInstancePropertyValues(Object instanceObj, XElement element)
        {
            if (instanceObj == null || element == null)
                throw new ArgumentNullException("参数不能为空");

            Object instanceFieldObj = GetInstanceFieldObject(instanceObj, element.Name.LocalName);
            if (instanceFieldObj == null) return;

            IEnumerable<XAttribute> attributes = element.Attributes();
            if (attributes.Count() == 0) return;

            foreach (XAttribute attribute in attributes)
            {
                SetInstancePropertyValue(instanceFieldObj, attribute.Name.LocalName, attribute.Value);
            }
        }
        /// <summary>
        /// 动态的设置实例对象的多个属性值，跟据 配置文件 中读取对应实例的 key 属性值
        /// <para>nameSpace 格式指：[Object.]Property , Object 可为空</para>
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="nameSpace"></param>
        public static void SetInstancePropertyValues(Object instanceObj, String nameSpace)
        {
            if (instanceObj == null)
                throw new ArgumentNullException(nameof(instanceObj), "参数不能为空");

            if (String.IsNullOrWhiteSpace(nameSpace)) nameSpace = "";
            PropertyInfo[] properties = instanceObj.GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (!property.CanWrite || !property.CanRead) continue;

                String value = ConfigurationManager.AppSettings[$"{nameSpace}{property.Name}"];
                if (String.IsNullOrWhiteSpace(value)) continue;

                SetInstancePropertyValue(instanceObj, property.Name, value);
            }
        }

        /// <summary>
        /// 动态的设置实例对象的属性值, Only Support ValueType And ArrayType
        /// <para>属性是指实现了 get,set 方法的对象</para>
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="propertyName"></param>
        /// <param name="newValue"></param>
        /// <returns>如果设置成功，则返回 true, 否则返回 false </returns>
        public static bool SetInstancePropertyValue(Object instanceObj, String propertyName, Object newValue)
        {
            if (instanceObj == null || String.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException("参数不能为空");

            Type type = instanceObj.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite || !property.CanRead) return false;

            object convertValue = null;
            try
            {
                if (newValue == null || newValue.ToString().ToLower().Trim() == "null")
                {
                    property.SetValue(instanceObj, convertValue, null);
                    return true;
                }

                if (property.PropertyType.IsValueType)
                    convertValue = StringExtensions.ConvertParamsToValueType(property.PropertyType, newValue);
                else if (property.PropertyType.IsArray)
                    convertValue = StringExtensions.ConvertParamsToArrayType(property.PropertyType, (object[])newValue);
                else
                    convertValue = Convert.ChangeType(newValue, property.PropertyType);

                property.SetValue(instanceObj, convertValue, null);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"设置实例对象 {type} 的属性 {property} 值 {(newValue == null ? "null" : newValue)} 失败：{ex}");
            }

            return false;
        }
        /// <summary>
        /// 动态的获取实例对象的属性值
        /// <para>属性是指实现了 get,set 方法的对象</para>
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static object GetInstancePropertyValue(Object instanceObj, String propertyName)
        {
            if (instanceObj == null || String.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException("参数不能为空");

            Type type = instanceObj.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanRead) return null;

            return property.GetValue(instanceObj);
        }

        /// <summary>
        /// 动态移除实例对象指定的委托事件
        /// </summary>
        /// <param name="instanceObj">对象实例</param>
        /// <param name="eventName">对象事件名称</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RemoveInstanceEvent(object instanceObj, string eventName)
        {
            if (instanceObj == null || string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException("参数不能为空");

            //BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            BindingFlags bindingAttr = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;

            try
            {
                FieldInfo fields = instanceObj.GetType().GetField(eventName, bindingAttr);      //当前类类型中查找
                if (fields == null)
                {
                    fields = instanceObj.GetType().BaseType.GetField(eventName, bindingAttr);   //基类类型中查找
                    if (fields == null) return;
                }

                object value = fields.GetValue(instanceObj);
                if (value == null || !(value is Delegate)) return;

                Delegate anonymity = (Delegate)value;
                foreach (Delegate handler in anonymity.GetInvocationList())
                {
                    if (instanceObj.GetType().GetEvent(eventName) == null) continue;

                    instanceObj.GetType().GetEvent(eventName).RemoveEventHandler(instanceObj, handler);
                    if (Logger.IsDebugEnabled)
                        Logger.Debug($"Object({instanceObj.GetType()}) Remove Event: {eventName}({handler.Method.Name})");
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Remove Anonymous Events Error:{0}", ex);
            }
        }
        /// <summary>
        /// 动态移除实例对象所有委托事件
        /// </summary>
        /// <param name="instanceObj">对象实例</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RemoveInstanceEvents(object instanceObj)
        {
            if (instanceObj == null) throw new ArgumentNullException("参数不能为空");

            EventInfo[] events = instanceObj.GetType().GetEvents();
            foreach (EventInfo info in events)
                RemoveInstanceEvent(instanceObj, info.Name);
#if false
            EventInfo[] baseEvents = instanceObj.GetType().BaseType.GetEvents();
            foreach (EventInfo info in baseEvents)
                RemoveInstanceEvent(instanceObj, info.Name);
#endif
        }
        
        /// <summary>
        /// 动态调用对象的方法
        /// <para>按顺序查找方法：扩展方法 > 静态方法 > 实例方法</para>
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="methodInfo"></param>
        /// <param name="parameters"></param>
        /// <returns>调用 Method 的返回值</returns>
        public static object CallInstanceMethod(object instanceObj, MethodInfo methodInfo, params object[] parameters)
        {
            if (methodInfo == null) return null;

            ParameterInfo[] parameterInfos = methodInfo.GetParameters();

            String paramDebugInfo = "";
            if (parameterInfos.Length > 0)
            {
                foreach (ParameterInfo info in parameterInfos)
                    paramDebugInfo += info.ToString() + ", ";
                paramDebugInfo = paramDebugInfo.Substring(0, paramDebugInfo.Length - 2);
            }

            object[] _parameters = null;
            int paramsLength = parameters == null ? 0 : parameters.Length;

            if(instanceObj != null)
            {
                //实例方法
                if (!methodInfo.IsStatic)
                {
                    _parameters = parameters;
                    Logger.Info($"实例对象 {instanceObj}, 准备执行匹配的函数(实例函数) {methodInfo.Name}({paramDebugInfo}), 参数 {parameterInfos.Length}/{paramsLength} 个");
                }
                //扩展方法
                else
                {
                    _parameters = new object[paramsLength + 1];
                    _parameters[0] = instanceObj;
                    for (int i = 0; i < paramsLength; i++) _parameters[i + 1] = parameters[i];

                    Logger.Info($"实例对象 {instanceObj}, 准备执行匹配的函数(扩展函数) {methodInfo.Name}({paramDebugInfo}), 参数 {parameterInfos.Length}/{_parameters?.Length} 个");
                }
            }
            //静态方法
            else
            {
                if (methodInfo.IsStatic)
                {
                    _parameters = parameters;
                    Logger.Info($"准备执行匹配的函数(静态函数) {methodInfo.Name}({paramDebugInfo}) 参数 {parameterInfos.Length}/{paramsLength} 个");
                }
                else
                {
                    Logger.Warn($"实例对象 {instanceObj}, 匹配的函数(超出处理范围的函数) {methodInfo.Name}({paramDebugInfo}), 参数 {parameterInfos.Length}/{_parameters?.Length} 个");
                    return null;
                }
            }

            object[] arguments = _parameters == null ? null : new object[_parameters.Length];
            try
            {
                //参数解析、转换
                for (int i = 0; i < _parameters?.Length; i++)
                {
                    ParameterInfo pInfo = parameterInfos[i];
                    if (_parameters[i] == null || _parameters[i].ToString().ToLower().Replace(" ", "") == "null")
                    {
                        arguments[i] = null;
                        continue;
                    }
#if false
                    if(Logger.IsDebugEnabled)
                        Logger.Debug($"Convert Type:: {_parameters[i].GetType()} / {pInfo.ParameterType}  IsValueType:{pInfo.ParameterType.IsValueType}  IsArray:{pInfo.ParameterType.IsArray}");
#endif
                    if (pInfo.ParameterType.IsValueType)
                    {
                        arguments[i] = StringExtensions.ConvertParamsToValueType(pInfo.ParameterType, _parameters[i]);
                    }
                    else if (pInfo.ParameterType.IsArray)
                    {
                        arguments[i] = StringExtensions.ConvertParamsToArrayType(pInfo.ParameterType, (object[])_parameters[i]);
                    }
                    else
                    {
                        arguments[i] = Convert.ChangeType(_parameters[i], pInfo.ParameterType);
                    }
                }

#if false
                if (Logger.IsDebugEnabled)
                {
                    foreach (object arg in arguments)
                        Logger.Debug($"{arg.GetType()} : {arg}");
                }
#endif
                return methodInfo.Invoke(instanceObj, arguments);
            }
            catch (Exception ex)
            {
                Logger.Warn($"函数 {methodInfo.Name} 执行失败: {ex}");
            }

            return null;
        }
        /// <summary>
        /// 动态调用 实例 对象的方法
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns>调用 Method 的返回值</returns>
        public static object CallInstanceMethod(object instanceObj, String methodName, params object[] parameters)
        {
            if (instanceObj == null || String.IsNullOrWhiteSpace(methodName))
                throw new ArgumentNullException("参数不能为空");

            int paramLength = parameters == null ? 0 : parameters.Length;

            Type type = instanceObj.GetType();
            List<MethodInfo> methods = (from method in type.GetMethods()
                                        where method.Name == methodName
                                        where method.GetParameters().Length == paramLength
                                        select method)?.ToList();

            //过滤一次，检查函数参数类型，ValueType==String, IsArray==IsArray
            if(methods?.Count > 1)
            {
                for(int i = 0; i < methods.Count; i ++)
                {
                    MethodInfo method = methods[i];
                    ParameterInfo[] paramsInfo = method.GetParameters();

                    for(int k = 0; k < paramsInfo.Length; k ++)
                    {
                        Type paramType = parameters[k].GetType();
                        if((paramsInfo[k].ParameterType.IsValueType && paramType == typeof(String)) || paramsInfo[k].ParameterType.IsArray == paramType.IsArray)
                        {
                            continue;
                        }

                        methods.Remove(method);
                        break;
                    }
                }
            }

            if (methods.Count != 1)
            {
                Logger.Warn($"在实例对象 {instanceObj} 中，找到匹配的函数 {methodName} 参数数量 {paramLength} 有 {methods.Count} 个，准备查找实例对象的扩展函数");

                return CallInstanceExtensionMethod(instanceObj, methodName, parameters);
            }
            
            return CallInstanceMethod(instanceObj, methods.First(), parameters);
        }

        /// <summary>
        /// 动态调用 对象扩展 的方法
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns>调用 Method 的返回值</returns>
        public static object CallInstanceExtensionMethod(object instanceObj, String methodName, params object[] parameters)
        {
            if (instanceObj == null || String.IsNullOrWhiteSpace(methodName))
                throw new ArgumentNullException("参数不能为空");

            List<MethodInfo> methods = (from type in typeof(InstanceExtensions).Assembly.GetTypes()
                                        where type.IsSealed && !type.IsGenericType && !type.IsNested
                                        from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                        where method.Name == methodName && method.IsDefined(typeof(ExtensionAttribute), false) && method.GetParameters()[0].ParameterType == instanceObj.GetType()
                                        select method).ToList();

            //过滤一次，检查函数参数类型，ValueType==String, IsArray==IsArray
            if (methods?.Count > 1)
            {
                for (int i = 0; i < methods.Count; i++)
                {
                    MethodInfo method = methods[i];
                    ParameterInfo[] paramsInfo = method.GetParameters();

                    for (int k = 0; k < paramsInfo.Length; k++)
                    {
                        Type paramType = parameters[k].GetType();
                        if ((paramsInfo[k].ParameterType.IsValueType && paramType == typeof(String)) || paramsInfo[k].ParameterType.IsArray == paramType.IsArray)
                        {
                            continue;
                        }

                        methods.Remove(method);
                        break;
                    }
                }
            }

            if (methods?.Count != 1)
            {
                Logger.Warn($"在实例对象 {instanceObj} 中，找到匹配的扩展函数 {methodName} 有 {methods?.Count} 个，取消执行");
                return null;
            }

            return CallInstanceMethod(instanceObj, methods.First(), parameters);
        }
        /// <summary>
        /// 动态调用 类的 静态方法
        /// <para>示例：InstanceExtension.CallClassStaticMethod("System.Threading.Thread", "Sleep", new object[] { "1000" });</para>
        /// </summary>
        /// <param name="classFullName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns>调用 Method 的返回值</returns>
        public static object CallClassStaticMethod(String classFullName, String methodName, params object[] parameters)
        {
            if (String.IsNullOrWhiteSpace(classFullName) || String.IsNullOrWhiteSpace(methodName)) return null;

            List<MethodInfo> methods = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                        let type = assembly.GetType(classFullName)
                                        where type != null
                                        from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                        where method.Name == methodName && method.GetParameters().Length == parameters.Length
                                        from paramInfo in method.GetParameters()
                                        where paramInfo.ParameterType.IsValueType || paramInfo.ParameterType.IsArray || paramInfo.ParameterType.IsEnum
                                        select method).ToList();

            //过滤一次，检查函数参数类型，ValueType==String, IsArray==IsArray
            if (methods?.Count > 1)
            {
                for (int i = 0; i < methods.Count; i++)
                {
                    MethodInfo method = methods[i];
                    ParameterInfo[] paramsInfo = method.GetParameters();

                    for (int k = 0; k < paramsInfo.Length; k++)
                    {
                        Type paramType = parameters[k].GetType();
                        if ((paramsInfo[k].ParameterType.IsValueType && paramType == typeof(String)) || paramsInfo[k].ParameterType.IsArray == paramType.IsArray)
                        {
                            continue;
                        }

                        methods.Remove(method);
                        break;
                    }
                }
            }

            int methodCount = methods == null ? 0 : methods.Count();
            if (methodCount != 1)
            {
                Logger.Warn($"在类 {classFullName} 中，找到匹配的静态函数 {methodName} 有 {methodCount} 个, 存在执行歧异, 取消函数执行");
                return null;
            }

            return CallInstanceMethod(null, methods.First(), parameters);
        }

        /// <summary>
        /// 试图解析 xml 格式消息，在 Object 字典中查找实例对象，并调用实例对象的方法
        /// <para>XML 格式："&lt;Action Target='object key name' Method='method name' Params='method params' /&gt;" 跟据调用的 Method 决定 Params 可选属性值</para>
        /// </summary>
        /// <param name="xmlMessage">xml 格式消息</param>
        /// <param name="objectsDictionary">可访问对象的集合</param>
        /// <param name="returnResult">Method 的返回值</param>
        /// <returns>Method 调用成功则返回 true, 反之返回 false</returns>
        public static bool TryParseCallMethod(String xmlMessage, IReadOnlyDictionary<String, IDisposable> objectsDictionary, out object returnResult)
        {
            returnResult = null;
            if (String.IsNullOrWhiteSpace(xmlMessage) || objectsDictionary == null) return false;

            XElement actionElement;

            try
            {
                actionElement = XElement.Parse(xmlMessage);
            }
            catch(Exception ex)
            {
                Logger.Warn($"XML 格式数据解析错误：{ex}");
                return false;
            }

            if(actionElement.Name?.LocalName != "Action")
            {
                Logger.Warn($"XML 格式数数据错误，节点名称应为 Action");
                return false;
            }

            return TryParseCallMethod(actionElement, objectsDictionary, out returnResult);
        }
        /// <summary>
        /// 试图解析 xml 格式消息，在 Object 字典找实例对象，并调用实例对象的方法
        /// <para>XML 格式：&lt;Action Target="object key name" Method="method name" Params="method params" /&gt; 跟据调用的 Method 决定 Params 可选属性值</para>
        /// </summary>
        /// <param name="actionElement"></param>
        /// <param name="objectsDictionary">可访问对象的集合</param>
        /// <param name="returnResult">Method 的返回值</param>
        /// <returns>Method 调用成功则返回 true, 反之返回 false</returns>
        public static bool TryParseCallMethod(XElement actionElement, IReadOnlyDictionary<String, IDisposable> objectsDictionary, out object returnResult)
        {
            returnResult = null;
            if (actionElement == null || objectsDictionary == null) return false;

            if (actionElement.Name?.LocalName != "Action")
            {
                Logger.Warn($"XML 格式数数据错误，节点名称应为 Action");
                return false;
            }

            try
            {
                if (String.IsNullOrWhiteSpace(actionElement.Attribute("Target")?.Value) ||
                    String.IsNullOrWhiteSpace(actionElement.Attribute("Method")?.Value)) return false;

                String objectName = actionElement.Attribute("Target").Value;
                String methodName = actionElement.Attribute("Method").Value;

                if (!objectsDictionary.TryGetValue(objectName, out IDisposable targetObject))
                {
                    Logger.Warn($"未找到目标实例对象 {objectName} ");
                    return false;
                }

                returnResult = Task.Run<Object>(() =>
                {
                    return InstanceExtensions.CallInstanceMethod(targetObject, methodName, StringExtensions.SplitParameters(actionElement.Attribute("Params")?.Value));
                });

                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"执行 XML 数据指令错误：{actionElement}");
                Logger.Error(ex);
            }

            return false;
        }
    }
}
