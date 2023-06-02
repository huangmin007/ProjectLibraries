#define RETURN

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using SpaceCG.Generic;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// 实例功能扩展库
    /// </summary>
    public static partial class InstanceExtensions
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(InstanceExtensions));

        static InstanceExtensions()
        {
            //嵌入 dll
        }

        /// <summary>
        /// 动态的获取实例的字段对象。注意：包括私有对象
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static object GetInstanceFieldValue(object instanceObj, String fieldName)
        {
            if (instanceObj == null || String.IsNullOrWhiteSpace(fieldName))
            {
#if RETURN
                return null;
#else
                throw new ArgumentNullException($"{nameof(instanceObj)},{nameof(fieldName)}", "参数不能为空");
#endif
            }

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
                Logger.Error(ex.ToString());
            }
            return null;
        }
        /// <summary>
        /// 动态的设置实例的字段对象。注意：包括私有对象
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="fieldName"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static bool SetInstanceFieldValue(object instanceObj, String fieldName, object newValue)
        {
            if (instanceObj == null || String.IsNullOrWhiteSpace(fieldName))
            {
#if RETURN
                return false;
#else
                throw new ArgumentNullException($"{nameof(instanceObj)},{nameof(fieldName)}", "参数不能为空");
#endif
            }

            Type type = instanceObj.GetType();
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                Logger.Warn($"在实例对象 {instanceObj} 中，未找到指定字段 {fieldName} 对象");
                return false;
            }

            object convertValue = null;
            try
            {
                if (newValue == null || newValue.ToString().ToLower().Trim() == "null")
                {
                    fieldInfo.SetValue(instanceObj, convertValue);
                    return true;
                }
                
                if (fieldInfo.FieldType.IsValueType)
                    convertValue = StringExtensions.ConvertParamsToValueType(fieldInfo.FieldType, newValue);
                else if (fieldInfo.FieldType.IsArray)
                    convertValue = StringExtensions.ConvertParamsToArrayType(fieldInfo.FieldType, (object[])newValue);
                else
                    convertValue = Convert.ChangeType(newValue, fieldInfo.FieldType);

                fieldInfo.SetValue(instanceObj, convertValue);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"设置实例对象 {type} 的字段 {fieldInfo} 值 {(newValue == null ? "null" : newValue)} 失败：{ex}");
            }

            return false;
        }


        /// <summary>
        /// 动态的设置实例对象的多个属性值，跟据 XML 配置文件节点名称(实例字段对象)及节点属性(字段对象的属性)来个修改实例属性
        /// <para>例如：&lt;Window Left="100" Width="100"/&gt; </para>
        /// <para> Window 是 instanceObj 中的一个实例对象(或变量对象，非静态对象)，其 Left、Width 为 Window 实例对象的原有属性 </para>
        /// </summary>
        /// <param name="instanceParentObj"></param>
        /// <param name="element"></param>
        public static void SetInstancePropertyValues(Object instanceParentObj, XElement element)
        {
            if (instanceParentObj == null || element == null)
            {
#if RETURN
                return;
#else
                throw new ArgumentNullException($"{nameof(instanceObj)},{nameof(element)}", "参数不能为空");
#endif
            }
            Object instanceObj = GetInstanceFieldValue(instanceParentObj, element.Name.LocalName);
            if (instanceObj == null) return;

            IEnumerable<XAttribute> attributes = element.Attributes();
            if (attributes.Count() == 0) return;

            foreach (XAttribute attribute in attributes)
            {
                SetInstancePropertyValue(instanceObj, attribute.Name.LocalName, attribute.Value);
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
            if (instanceObj == null || String.IsNullOrWhiteSpace(propertyName))
            {
#if RETURN
                return false;
#else
                throw new ArgumentNullException($"{nameof(instanceObj)},{nameof(propertyName)}", "参数不能为空");
#endif
            }

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
            if (instanceObj == null || String.IsNullOrWhiteSpace(propertyName))
            {
#if RETURN
                return false;
#else
                throw new ArgumentNullException($"{nameof(instanceObj)},{nameof(propertyName)}", "参数不能为空");
#endif
            }

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
            {
#if RETURN
                return;
#else
                throw new ArgumentNullException($"{nameof(instanceObj)},{nameof(eventName)}", "参数不能为空");
#endif
            }

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
                    Logger.Debug($"Object({instanceObj.GetType()}) Remove Event: {eventName}({handler.Method.Name})");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Remove Anonymous Events Error:{ex}");
            }
        }
        /// <summary>
        /// 动态移除实例对象所有委托事件
        /// </summary>
        /// <param name="instanceObj">对象实例</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RemoveInstanceEvents(object instanceObj)
        {
            if (instanceObj == null)
            {
#if RETURN
                return;
#else
                throw new ArgumentNullException(nameof(instanceObj), "参数不能为空");
#endif
            }

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
        /// 移出不匹配参数的 MethodInfo 对象
        /// <para>检查函数参数类型，ValueType==ValueType||String, IsArray==IsArray</para>
        /// </summary>
        /// <param name="methods"></param>
        /// <param name="parameters"></param>
        private static void MoveOutMethodInfo(ref List<MethodInfo> methods, params object[] parameters)
        {
            if (methods?.Count <= 1) return;

            for (int i = 0; i < methods.Count; i++)
            {
                MethodInfo method = methods[i];
                ParameterInfo[] paramsInfo = method.GetParameters();
                if (paramsInfo.Length != parameters.Length) continue;

                for (int k = 0; k < paramsInfo.Length; k++)
                {
                    Type paramType = parameters[k].GetType();
                    if ((paramsInfo[k].ParameterType.IsArray && paramType.IsArray) ||
                        (paramsInfo[k].ParameterType.IsValueType && (paramType.IsValueType || paramType == typeof(String))))
                    {
                        continue;
                    }

                    methods.Remove(method);
                    break;
                }
            }
        }
        /// <summary>
        /// 动态调用对象的方法
        /// <para>按顺序查找方法：实例方法 > 扩展方法 > 静态方法 </para>
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="methodInfo"></param>
        /// <param name="parameters"></param>
        /// <returns>调用 Method 的返回值</returns>
        public static object CallInstanceMethod(object instanceObj, MethodInfo methodInfo, params object[] parameters)
        {
            if (methodInfo == null)
            {
#if RETURN
                return null;
#else
                throw new ArgumentNullException(nameof(methodInfo), "参数不能为空");
#endif
            }

            String paramDebugInfo = "";
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
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
                        Console.WriteLine($"{arg.GetType()} : {arg}");
                }
#endif
                return methodInfo.Invoke(instanceObj, arguments);
            }
            catch (Exception ex)
            {
                Logger.Error($"函数 {methodInfo.Name} 执行失败: {ex}");
            }

            return null;
        }
        /// <summary>
        /// 动态调用 实例 对象的方法
        /// <para>按顺序查找方法：实例方法 > 实例的扩展方法</para>
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns>调用 Method 的返回值</returns>
        public static object CallInstanceMethod(object instanceObj, String methodName, params object[] parameters)
        {
            if (instanceObj == null || String.IsNullOrWhiteSpace(methodName))
            {
#if RETURN
                return null;
#else
                throw new ArgumentNullException($"{nameof(instanceObj)},{nameof(methodName)}", "参数不能为空");
#endif
            }

            Type type = instanceObj.GetType();
            int paramLength = parameters == null ? 0 : parameters.Length;
            List<MethodInfo> methods = (from method in type.GetMethods()
                                        where method.Name == methodName
                                        where method.GetParameters().Length == paramLength
                                        select method)?.ToList();

            MoveOutMethodInfo(ref methods);
            int methodCount = methods == null ? 0 : methods.Count();

            if (methodCount != 1)
            {
                Logger.Info($"在实例对象 {instanceObj} 中，找到匹配的函数 {methodName}/{paramLength} 有 {methodCount} 个，准备查找实例对象的扩展函数");
                return CallInstanceExtensionMethod(instanceObj, methodName, parameters);
            }
            
            return CallInstanceMethod(instanceObj, methods.First(), parameters);
        }

        /// <summary>
        /// 动态调用 对象扩展 的方法
        /// <para>按顺序查找方法：实例的扩展方法 </para>
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns>调用 Method 的返回值</returns>
        public static object CallInstanceExtensionMethod(object instanceObj, String methodName, params object[] parameters)
        {
            if (instanceObj == null || String.IsNullOrWhiteSpace(methodName))
            {
#if RETURN
                return null;
#else
                throw new ArgumentNullException($"{nameof(instanceObj)},{nameof(methodName)}", "参数不能为空");
#endif
            }

            List<MethodInfo> methods = (from type in typeof(InstanceExtensions).Assembly.GetTypes()
                                        where type.IsSealed && !type.IsGenericType && !type.IsNested
                                        from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                        where method.Name == methodName && method.IsDefined(typeof(ExtensionAttribute), false) && method.GetParameters()[0].ParameterType == instanceObj.GetType()
                                        select method).ToList();

            MoveOutMethodInfo(ref methods);
            int methodCount = methods == null ? 0 : methods.Count();

            if (methodCount != 1)
            {
                Logger.Warn($"在实例对象 {instanceObj} 中，找到匹配的扩展函数 {methodName} 有 {methodCount} 个，取消执行");
                return null;
            }

            return CallInstanceMethod(instanceObj, methods.First(), parameters);
        }
        /// <summary>
        /// 动态调用 类的 静态方法
        /// <para>按顺序查找方法：静态方法</para>
        /// <para>示例：InstanceExtension.CallClassStaticMethod("System.Threading.Thread", "Sleep", new object[] { "1000" });</para>
        /// </summary>
        /// <param name="classFullName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns>调用 Method 的返回值</returns>
        public static object CallClassStaticMethod(String classFullName, String methodName, params object[] parameters)
        {
            if (String.IsNullOrWhiteSpace(classFullName) || String.IsNullOrWhiteSpace(methodName))
            {
#if RETURN
                return null;
#else
                throw new ArgumentNullException($"{nameof(classFullName)},{nameof(methodName)}", "参数不能为空");
#endif
            }

            List<MethodInfo> methods = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                        let type = assembly.GetType(classFullName)
                                        where type != null
                                        from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                        where method.Name == methodName && method.GetParameters().Length == parameters.Length
                                        from paramInfo in method.GetParameters()
                                        where paramInfo.ParameterType.IsValueType || paramInfo.ParameterType.IsArray || paramInfo.ParameterType.IsEnum
                                        select method).ToList();

            MoveOutMethodInfo(ref methods);
            int methodCount = methods == null ? 0 : methods.Count();

            if (methodCount != 1)
            {
                Logger.Warn($"在类 {classFullName} 中，找到匹配的静态函数 {methodName} 有 {methodCount} 个, 存在执行歧异, 取消函数执行");
                return null;
            }

            return CallInstanceMethod(null, methods.First(), parameters);
        }

    }
}
