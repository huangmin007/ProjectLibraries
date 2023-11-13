using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using SpaceCG.Generic;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// Type Extensions
    /// </summary>
    public static partial class TypeExtensions
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(TypeExtensions));

        /// <summary>
        /// 封装一个方法，该方法输出一个指定类型的对象, 该对象的值等效于指定的对象
        /// </summary>
        /// <param name="value">需要转换的对象、字符串或是字符串描述</param>
        /// <param name="destinationType">表示希望转换为的类型</param>
        /// <param name="conversionValue">输出一个转换后的对象，其类型为 destinationType，并且其值等效于 value </param>
        /// <returns> 输出类型的值 conversionValue 为有效对象时，返回 true, 否则返回 false </returns>
        public delegate bool TypeConverterDelegate(object value, Type destinationType, out object conversionValue);

        /// <summary>
        /// 扩展类型转换代理函数, 输出一个指定类型的对象, 该对象的值等效于指定的对象。
        /// <para>当通过调用反射来设置属性值或是调用反射带参的函数时，值类型转换失败或异常时，调用该代理函数进行扩展类型转换，完善动态反射功能</para>
        /// </summary>
        public static TypeConverterDelegate CustomConvertFromExtension;

        /// <summary>
        /// 指示其参数是否为数值类型
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumeric(object value)
        {
            Type valueType = value is Type ? (Type)value : value?.GetType();
            return valueType == typeof(SByte) || valueType == typeof(Byte) || valueType == typeof(Int16) || valueType == typeof(UInt16) ||
                   valueType == typeof(Int32) || valueType == typeof(UInt32) || valueType == typeof(Int64) || valueType == typeof(UInt64) ||
                   valueType == typeof(Single) || valueType == typeof(Double) || valueType == typeof(Decimal);// || valueType == typeof(BigInteger);
        }

        /// <summary>
        /// 指示其参数是否为整数类型
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInteger(object value)
        {
            Type valueType = value is Type ? (Type)value : value?.GetType();
            return valueType == typeof(SByte) || valueType == typeof(Byte) || valueType == typeof(Int16) || valueType == typeof(UInt16) ||
                   valueType == typeof(Int32) || valueType == typeof(UInt32) || valueType == typeof(Int64) || valueType == typeof(UInt64);// || valueType == typeof(BigInteger);
        }

        /// <summary>
        /// 指示其参数是否为浮点数类型
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFloat(object value)
        {
            Type valueType = value is Type ? (Type)value : value?.GetType();
            return valueType == typeof(Single) || valueType == typeof(Double) || valueType == typeof(Decimal);
        }

        /// <summary>
        /// 将给定的值转换为目标类型对象。
        /// </summary>
        /// <param name="value">需要转换的对象，字符串或字符串描述</param>
        /// <param name="destinationType">表示希望转换为的类型</param>
        /// <param name="conversionValue">输出一个转换后的对象，其类型为 destinationType，并且其值等效于 value </param>
        /// <returns>输出类型的值 conversionValue 为有效对象时，返回 true, 否则返回 false </returns>
        public static bool ConvertFrom(object value, Type destinationType, out object conversionValue)
        {
            conversionValue = null;
            if (destinationType == null) return false;
            if (value == null || destinationType == typeof(void)) return true;

            #region 扩展部份字符类型的转换
            Type valueType = value.GetType();
            if (!destinationType.IsArray && valueType == destinationType)
            {
                conversionValue = value;
                return true;
            }
            else if (!destinationType.IsArray && valueType == typeof(string))
            {
                string valueString = value.ToString();
                // 0.空白或字符 "null" 转换为对象 null
                if (string.IsNullOrWhiteSpace(valueString) || valueString.ToLower().Trim() == "null") return true;

                /* string to object
                 * 1.枚举类型转换
                 * 1.1 扩展支持字符 "{枚举类型}.{枚举值}" 的转换
                 * 1.2 扩展支持数字或字符格式 "数字"或"0x数字" 的转换，识别 "0x" 为十六进制数的转换
                 * 2.Boolean类型转换
                 * 2.1 字符或数字 "1" 和 字母 "T" 的转换，为 true，其它为 false
                 * 3.数值类型的转换
                 * 3.1 字符或数字格式 "数字"或"0x数字" 的转换，识别 "0x" 为十六进制数的转换
                 */
                if (destinationType.IsEnum)
                {
                    valueString = valueString.ToLower().Trim();
                    string desTypeName = $"{destinationType.Name}.".ToLower();
                    if (valueString.IndexOf(desTypeName) == 0) valueString = valueString.Replace(desTypeName, "");
                    if (valueString.IndexOf("0x") == 0 && StringExtensions.ToNumber(valueString, out int number)) valueString = number.ToString();

                    try
                    {
                        conversionValue = Enum.Parse(destinationType, valueString, true);
                        return true;
                    }
                    catch (Exception ex) 
                    { 
                        Logger.Warn($"{ex}");
                        return false;
                    }
                }
                else if (destinationType == typeof(bool))
                {
                    if (bool.TryParse(valueString, out bool boolValue))
                    {
                        conversionValue = boolValue;
                        return true;
                    }
                    string pv = valueString.Replace(" ", "");
                    conversionValue = pv == "1" || pv == "T";
                    return true;
                }
                else if (IsNumeric(destinationType))
                {
                    if (StringExtensions.ToNumber(valueString, destinationType, out ValueType numberValue))
                    {
                        conversionValue = numberValue;
                        return true;
                    }
                }
            }
            else if (destinationType.IsArray && valueType.IsArray && destinationType.GetElementType() == valueType.GetElementType())
            {
                conversionValue = value;
                return true;
            }
            else if (destinationType.IsArray && valueType.IsArray && destinationType.GetElementType() != valueType.GetElementType())
            {
                Array valueArray = (Array)value;
                Type elementType = destinationType.GetElementType();
                Array instanceValue = Array.CreateInstance(elementType, valueArray.Length);

                for (int i = 0; i < valueArray.Length; i++)
                {
                    if (!ConvertFrom(valueArray.GetValue(i), elementType, out object cValue)) continue;
                    instanceValue.SetValue(cValue, i);
                }
                conversionValue = instanceValue;
                return true;
            }
            #endregion

            try
            {
                TypeConverter converter = TypeDescriptor.GetConverter(destinationType);
                if (converter != null && converter.CanConvertFrom(valueType))
                {
                    conversionValue = valueType == typeof(string) ? converter.ConvertFromString(value.ToString()) : converter.ConvertFrom(value);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex}");
            }

            if (CustomConvertFromExtension != null)
                return CustomConvertFromExtension.Invoke(value, destinationType, out conversionValue);

            Logger.Warn($"类型转换失败  Value:{value}  Type:{destinationType}");
            return false;
        }

        /// <summary>
        /// 将给定的值转换为目标类型对象。
        /// </summary>
        /// <typeparam name="T">表示希望转换为的类型</typeparam>
        /// <param name="value">需要转换的对象，字符串或字符串描述</param>
        /// <param name="conversionValue">输出一个转换后的对象，其类型为 destinationType，并且其值等效于 value </param>
        /// <returns>输出类型的值 conversionValue 为有效对象时，返回 true, 否则返回 false </returns>
        public static bool ConvertFrom<T>(object value, out T conversionValue) // where T : notnull
        {
            conversionValue = default;
            Type destinationType = typeof(T);

            bool result = ConvertFrom(value, destinationType, out object convertValue);
            if (result) conversionValue = (T)convertValue;

            return result;
        }

#if false
        /// <summary>
        /// 比较两个类型的集合是否相同
        /// </summary>
        /// <param name="sourceTypes"></param>
        /// <param name="targetTypes"></param>
        /// <returns>如果两个类型的集合相同，则返回 true，否则返回 false </returns>
        [Obsolete("建议使用 IEnumerable<T>.SequenceEqual(IEnumerable<T>)", false)]
        public static bool Equals(IEnumerable<Type> sourceTypes, IEnumerable<Type> targetTypes)
        //public static bool Equals(IReadOnlyCollection<Type> sourceTypes, IReadOnlyCollection<Type> targetTypes)
        {
            if (sourceTypes == null || targetTypes == null) return false;
            if (sourceTypes.Count() == 0 || sourceTypes.Count() != targetTypes.Count()) return false;
            if (sourceTypes == targetTypes) return true;
            
            int count = sourceTypes.Count();
            for (int i = 0; i < count; i++)
            {
                if (sourceTypes.ElementAt(i) != targetTypes.ElementAt(i)) return false;
            }

            return true;
        }
#endif
    }
}
