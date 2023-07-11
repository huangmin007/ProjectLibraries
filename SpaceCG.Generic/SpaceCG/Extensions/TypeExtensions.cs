using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using SpaceCG.Generic;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// Type Extensions
    /// </summary>
    public static class TypeExtensions
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(TypeExtensions));

        /// <summary>
        /// 指示其参数是否为数字类型
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsNumeric(object value)
        {
            if (value == null) return false;

            Type valueType = value is Type ? (Type)value : value.GetType();
            return valueType == typeof(SByte) || valueType == typeof(Byte) || valueType == typeof(Int16) || valueType == typeof(UInt16) ||
                   valueType == typeof(Int32) || valueType == typeof(UInt32) || valueType == typeof(Int64) || valueType == typeof(UInt64) ||
                   valueType == typeof(Single) || valueType == typeof(Double) || valueType == typeof(Decimal);// || valueType == typeof(BigInteger);
        }

        /// <summary>
        /// 指示其参数是否为整数类型
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsInteger(object value)
        {
            if (value == null) return false;

            Type valueType = value is Type ? (Type)value : value.GetType();
            return valueType == typeof(SByte) || valueType == typeof(Byte) || valueType == typeof(Int16) || valueType == typeof(UInt16) ||
                   valueType == typeof(Int32) || valueType == typeof(UInt32) || valueType == typeof(Int64) || valueType == typeof(UInt64);// || valueType == typeof(BigInteger);
        }

        /// <summary>
        /// 指示其参数是否为浮点数类型
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsFloat(object value)
        {
            if (value == null) return false;

            Type valueType = value is Type ? (Type)value : value.GetType();
            return valueType == typeof(Single) || valueType == typeof(Double) || valueType == typeof(Decimal);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value">需要转换的对象、字符串或字符串描述</param>
        /// <param name="destinationType">需要转换的目标类型，非数组类型</param>
        /// <param name="conversionValue">转换后的对象</param>
        /// <returns> 输出类型的值 conversionValue 为有效对象返回 true, 否则返回 false </returns>
        public static bool ConvertTo(object value, Type destinationType, out object conversionValue)
        {
            conversionValue = null;
            if (value == null) return true;
            if (destinationType == null || destinationType.IsArray)
                throw new ArgumentException(nameof(destinationType), "需要转换的类型不能为空，或不能为数组类型");

            Type valueType = value.GetType();
            if (valueType == destinationType)
            {
                conversionValue = value;
                return true;
            }

            Type stringType = typeof(string);
            if (valueType == stringType)
            {
                string valueString = value.ToString();
                if (string.IsNullOrWhiteSpace(valueString) || valueString.ToLower().Trim() == "null") return true;

                if (destinationType.IsEnum)
                {
                    try
                    {
                        conversionValue = Enum.Parse(destinationType, valueString, true);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex.ToString());
                        return false;
                    }
                }
                else if(destinationType == typeof(bool))
                {
                    if (bool.TryParse(valueString, out bool result))
                    {
                        conversionValue = result;
                        return true;
                    }
                    string pv = valueString.Replace(" ", "");
                    conversionValue = pv == "1" || pv == "T";
                    return true;
                }
                else if(IsNumeric(destinationType))
                {
                    
                }
                else
                {                    
                }
            }

            try
            {
                TypeConverter converter = TypeDescriptor.GetConverter(destinationType);
                conversionValue = valueType == stringType ? converter.ConvertFromString(value.ToString()) : converter.ConvertFrom(value);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.ToString()}");
                return false;
            }
        }

    }
}
