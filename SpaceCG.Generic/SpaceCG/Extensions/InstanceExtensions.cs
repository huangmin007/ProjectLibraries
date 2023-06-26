﻿using System;
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
        /// <summary>
        /// Logger Trace
        /// </summary>
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(InstanceExtensions));

        /// <summary>
        /// 封装一个方法，该方法输出一个指定类型的对象, 该对象的值等效于指定的对象
        /// </summary>
        /// <param name="value">需要转换的对象、字符串或是字符串描述</param>
        /// <param name="conversionType">要返回的对象的类型</param>
        /// <param name="conversionValue">返回一个对象，其类型为 conversionType，并且其值等效于 value </param>
        /// <returns>输出类型的值 conversionValue 为有效对象返回 true, 否则返回 false </returns>
        public delegate bool ConvertChangeTypeDelegate(object value, Type conversionType, out object conversionValue);

        /// <summary>
        /// 扩展类型转换代理函数, 输出一个指定类型的对象, 该对象的值等效于指定的对象。
        /// <para>当通过调用反射来设置属性值或是调用反射带参的函数时，值类型转换失败或异常时，调用该代理函数进行扩展类型转换，完善动态反射功能</para>
        /// </summary>
        public static ConvertChangeTypeDelegate ConvertChangeTypeExtension;

        /// <summary>
        /// 输出一个指定类型的对象, 该对象的值等效于指定的对象。
        /// </summary>
        /// <param name="value">需要转换的对象、字符串或是字符串描述</param>
        /// <param name="conversionType">要返回的对象的类型</param>
        /// <param name="conversionValue">返回一个对象，其类型为 conversionType，并且其值等效于 value </param>
        /// <returns>输出类型的值 conversionValue 为有效对象返回 true, 否则返回 false </returns>
        public static bool ConvertChangeType(object value, Type conversionType, out object conversionValue)
        {
            conversionValue = null;
            if (conversionType == null) return false;

            if (value == null) return true;
            if (value.GetType() == typeof(string))
            {
                string valueString = value.ToString();
                if (string.IsNullOrWhiteSpace(valueString) || valueString.Replace(" ", "").ToLower() == "null") return true;
            }

            if (value.GetType() == conversionType)
            {
                conversionValue = value;
                return true;
            }
            
            if (conversionType.IsValueType || conversionType.IsEnum)
            {
                bool result = StringExtensions.ConvertChangeTypeToValueType(value, conversionType, out ValueType cValue);
                if (result) conversionValue = cValue;
                return result;
            }
            else if (conversionType.IsArray && conversionType.HasElementType && conversionType.GetElementType().IsValueType)
            {
                bool result = StringExtensions.ConvertChangeTypeToArrayType(value as Array, conversionType, out Array cValue);
                if (result) conversionValue = cValue;
                return result;
            }
            else
            {
                try
                {
                    if (typeof(IConvertible).IsAssignableFrom(value.GetType()))
                    {
                        conversionValue = Convert.ChangeType(value, conversionType);
                        return true;
                    }
                    //扩展转换
                    if (ConvertChangeTypeExtension != null)
                    {
                        bool result = ConvertChangeTypeExtension.Invoke(value, conversionType, out conversionValue);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"{(ConvertChangeTypeExtension == null ? "类型" : "扩展类型")}转换失败  Value:{value}  Type:{conversionType}");
                    Logger.Error(ex);
                }
            }

            return false;
        }

        /// <summary>
        /// 试图动态的获取实例的字段对象。注意：包括私有对象
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <returns>获取成功返回 true, 否则返回 false </returns>
        public static bool GetInstanceFieldValue(object instanceObj, String fieldName, out object value)
        {
            value = null;
            if (instanceObj == null || String.IsNullOrWhiteSpace(fieldName)) return false;

            try
            {
                Type type = instanceObj.GetType();
                FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (fieldInfo == null)
                {
                    Logger.Warn($"在实例对象 {instanceObj} 中，未找到指定字段 {fieldName} 对象");
                    return false;
                }

                value = fieldInfo.GetValue(instanceObj);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
            return false;
        }
        /// <summary>
        /// 试图动态的设置实例的字段对象。注意：包括私有对象
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <returns>设置成功返回 true, 否则返回 false </returns>
        public static bool SetInstanceFieldValue(object instanceObj, String fieldName, object value)
        {
            if (instanceObj == null || String.IsNullOrWhiteSpace(fieldName)) return false;

            Type type = instanceObj.GetType();
            FieldInfo fieldInfo = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                Logger.Warn($"在实例对象 {instanceObj} 中，未找到指定字段 {fieldName} 对象");
                return false;
            }

            if(ConvertChangeType(value, fieldInfo.FieldType, out object convertValue))
            {
                try
                {
                    fieldInfo.SetValue(instanceObj, convertValue);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error($"设置实例对象 {type} 的字段 {fieldInfo} 值 {(value == null ? "null" : value)} 失败：{ex}");
                }
            }

            return false;
        }

        /// <summary>
        /// 动态的设置实例对象 (字段或私有字段) 的多个属性值，跟据 XML 配置文件节点名称(实例字段对象)及节点属性(字段对象的属性)来个修改实例属性
        /// <para>例如：&lt;Window Left="100" Width="100"/&gt; </para>
        /// <para> Window 是 instanceObj 中的一个实例字段对象(非静态)，其 Left、Width 为 Window 实例字段对象的原有属性 </para>
        /// </summary>
        /// <param name="instanceParentObj"></param>
        /// <param name="element"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void SetInstancePropertyValues(Object instanceParentObj, XElement element)
        {
            if (instanceParentObj == null || element == null)
                throw new ArgumentNullException($"{nameof(instanceParentObj)},{nameof(element)}", "参数不能为空");

            if(!GetInstanceFieldValue(instanceParentObj, element.Name.LocalName, out object instanceObj)) return;

            IEnumerable<XAttribute> attributes = element.Attributes();
            if (attributes.Count() == 0) return;

            foreach (XAttribute attribute in attributes)
            {
                //SetInstanceFieldValue(instanceObj, attribute.Name.LocalName, attribute.Value);
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
        /// <returns>设置成功返回 true, 否则返回 false </returns>
        public static bool SetInstancePropertyValue(Object instanceObj, String propertyName, Object newValue)
        {
            if (instanceObj == null || String.IsNullOrWhiteSpace(propertyName)) return false;

            Type type = instanceObj.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite || !property.CanRead) return false;

            if (ConvertChangeType(newValue, property.PropertyType, out object convertValue))
            {
                try
                {
                    property.SetValue(instanceObj, convertValue, null);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error($"设置实例对象 {type} 的属性 {property} 值 {(newValue == null ? "null" : newValue)} 失败：{ex}");
                }
            }

            return false;
        }
        /// <summary>
        /// 动态的获取实例对象的属性值
        /// <para>属性是指实现了 get,set 方法的对象</para>
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <returns>获取成功返回 true, 否则返回 false </returns>
        public static bool GetInstancePropertyValue(Object instanceObj, String propertyName, out object value)
        {
            value = null;
            if (instanceObj == null || String.IsNullOrWhiteSpace(propertyName)) return false;

            Type type = instanceObj.GetType();
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanRead) return false;

            try
            {
                value = property.GetValue(instanceObj);
                return true;
            }
            catch(Exception ex)
            {
                Logger.Error(ex);
            }

            return false;
        }

        /// <summary>
        /// 动态移除实例对象指定的委托事件
        /// </summary>
        /// <param name="instanceObj">对象实例</param>
        /// <param name="eventName">对象事件名称</param>
        /// <returns>移除成功返回 true, 否则返回 false </returns>
        public static bool RemoveInstanceEvent(object instanceObj, string eventName)
        {
            if (instanceObj == null || string.IsNullOrWhiteSpace(eventName)) return false;

            //BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            BindingFlags bindingAttr = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;

            try
            {
                FieldInfo fields = instanceObj.GetType().GetField(eventName, bindingAttr);      //当前类类型中查找
                if (fields == null)
                {
                    fields = instanceObj.GetType().BaseType.GetField(eventName, bindingAttr);   //基类类型中查找
                    if (fields == null) return false;
                }

                object value = fields.GetValue(instanceObj);
                if (value == null || !(value is Delegate)) return false;

                Delegate anonymity = (Delegate)value;
                foreach (Delegate handler in anonymity.GetInvocationList())
                {
                    if (instanceObj.GetType().GetEvent(eventName) == null) continue;

                    instanceObj.GetType().GetEvent(eventName).RemoveEventHandler(instanceObj, handler);
                    if(Logger.IsDebugEnabled) Logger.Debug($"Object({instanceObj.GetType()}) Remove Event: {eventName}({handler.Method.Name})");
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Remove Anonymous Events Error:{ex}");
            }
            return false;
        }
        /// <summary>
        /// 动态移除实例对象所有委托事件
        /// </summary>
        /// <param name="instanceObj">对象实例</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RemoveInstanceEvents(object instanceObj)
        {
            if (instanceObj == null) 
                throw new ArgumentNullException(nameof(instanceObj), "参数不能为空");

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
        private static void MoveOutMethodInfo(ref List<MethodInfo> methods, object[] parameters)
        {
            if (methods?.Count <= 1) return;

            for (int i = 0; i < methods.Count; i++)
            {
                MethodInfo method = methods[i];
                ParameterInfo[] paramsInfo = method.GetParameters();
                if (paramsInfo.Length != parameters?.Length) continue;

                for (int k = 0; k < paramsInfo.Length; k++)
                {
                    Type inputParamType = parameters[k].GetType();
                    Type methodParamType = paramsInfo[k].ParameterType;

                    if ((methodParamType.IsArray && inputParamType.IsArray) ||
                        (methodParamType.IsValueType && (inputParamType.IsValueType || inputParamType == typeof(String))))
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
        /// <para>按顺序查找方法：实例方法 > (实例)扩展方法 > (类)静态方法 </para>
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="methodInfo"></param>
        /// <param name="parameters"></param>
        /// <param name="returnValue">函数返回值</param>
        /// <returns>方法调用成功返回 true, 否则返回 false </returns>
        public static bool CallInstanceMethod(object instanceObj, MethodInfo methodInfo, object[] parameters, out object returnValue)
        {
            returnValue = null;
            if (methodInfo == null) return false;

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

            if (instanceObj != null)
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
                    return false;
                }
            }

            object[] arguments = _parameters == null ? null : new object[_parameters.Length];

            //参数解析、转换
            for (int i = 0; i < _parameters?.Length; i++)
            {
                if (!ConvertChangeType(_parameters[i], parameterInfos[i].ParameterType, out object convertValue)) return false;
                arguments[i] = convertValue;
            }

            try
            {
                returnValue = methodInfo.Invoke(instanceObj, arguments);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"函数 {methodInfo.Name} 执行失败: {ex}");
            }

            return false;
        }
        /// <summary>
        /// 动态调用 实例 对象的方法
        /// <para>按顺序查找方法：实例方法 > (实例)扩展方法</para>
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="returnValue">函数的返回值</param>
        /// <returns>方法调用成功返回 true, 否则返回 false</returns>
        public static bool CallInstanceMethod(object instanceObj, String methodName, object[] parameters, out object returnValue)
        {
            returnValue = null;
            if (instanceObj == null || String.IsNullOrWhiteSpace(methodName)) return false;

            Type type = instanceObj.GetType();
            int paramLength = parameters == null ? 0 : parameters.Length;
            List<MethodInfo> methods = (from method in type.GetMethods()
                                        where method.Name == methodName
                                        where method.GetParameters().Length == paramLength
                                        select method)?.ToList();

            MoveOutMethodInfo(ref methods, parameters);
            int methodCount = methods == null ? 0 : methods.Count();

            if (methodCount != 1)
            {
                Logger.Info($"在实例对象 {instanceObj} 中，找到匹配的函数 {methodName}/{paramLength} 有 {methodCount} 个，准备查找实例对象的扩展函数");
                return CallInstanceExtensionMethod(instanceObj, methodName, parameters, out returnValue);
            }
            
            return CallInstanceMethod(instanceObj, methods.First(), parameters, out returnValue);
        }

        /// <summary>
        /// 动态调用 对象扩展 的方法
        /// <para>按顺序查找方法：(实例)扩展方法 </para>
        /// </summary>
        /// <param name="instanceObj"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="returnValue">函数的返回值</param>
        /// <returns>方法调用成功返回 true, 否则返回 false</returns>
        public static bool CallInstanceExtensionMethod(object instanceObj, String methodName, object[] parameters, out object returnValue)
        {
            returnValue = null;
            if (instanceObj == null || String.IsNullOrWhiteSpace(methodName)) return false;

            List<MethodInfo> methods = (from type in typeof(InstanceExtensions).Assembly.GetTypes()
                                        where type.IsSealed && !type.IsGenericType && !type.IsNested
                                        from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                        where method.Name == methodName && method.IsDefined(typeof(ExtensionAttribute), false) && method.GetParameters()[0].ParameterType == instanceObj.GetType()
                                        select method).ToList();

            MoveOutMethodInfo(ref methods, parameters);
            int methodCount = methods == null ? 0 : methods.Count();

            if (methodCount != 1)
            {
                Logger.Warn($"在实例对象 {instanceObj} 中，找到匹配的扩展函数 {methodName} 有 {methodCount} 个，取消执行");
                return false;
            }

            return CallInstanceMethod(instanceObj, methods.First(), parameters, out returnValue);
        }
        /// <summary>
        /// 动态调用 类的 静态方法
        /// <para>按顺序查找方法：(类)静态方法</para>
        /// <para>示例：InstanceExtension.CallClassStaticMethod("System.Threading.Thread", "Sleep", new object[] { "1000" });</para>
        /// </summary>
        /// <param name="classFullName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="returnValue">函数的返回值</param>
        /// <returns>方法调用成功返回 true, 否则返回 false</returns>
        public static bool CallClassStaticMethod(String classFullName, String methodName, object[] parameters, out object returnValue)
        {
            returnValue = null;
            if (String.IsNullOrWhiteSpace(classFullName) || String.IsNullOrWhiteSpace(methodName)) return false;

            List<MethodInfo> methods = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                        let type = assembly.GetType(classFullName)
                                        where type != null
                                        from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                        where method.Name == methodName && method.GetParameters().Length == parameters.Length
                                        from paramInfo in method.GetParameters()
                                        where paramInfo.ParameterType.IsValueType || paramInfo.ParameterType.IsArray || paramInfo.ParameterType.IsEnum
                                        select method).ToList();

            MoveOutMethodInfo(ref methods, parameters);
            int methodCount = methods == null ? 0 : methods.Count();

            if (methodCount != 1)
            {
                Logger.Warn($"在类 {classFullName} 中，找到匹配的静态函数 {methodName} 有 {methodCount} 个, 存在执行歧异, 取消函数执行");
                return false;
            }

            return CallInstanceMethod(null, methods.First(), parameters, out returnValue);
        }

    }
}
