using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SpaceCG.Generic
{
    public static partial class StringExtensions
    {
        /// <summary>
        /// log4net.Logger 对象
        /// </summary>
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(nameof(StringExtensions));

        /// <summary>
        /// 将数字字符类型型转为数值类型
        /// </summary>
        /// <typeparam name="NumberType"></typeparam>
        /// <param name="value"></param>
        /// <param name="number"></param>
        /// <param name="style"></param>
        public static bool TryParse<NumberType>(this String value, out NumberType number, NumberStyles style = NumberStyles.None)
            where NumberType : struct, IComparable, IFormattable, IConvertible, IComparable<NumberType>, IEquatable<NumberType>
        {
            number = default;
            if (String.IsNullOrWhiteSpace(value)) return false;

            IEnumerable<MethodInfo> methods = from method in typeof(NumberType).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                              where method.Name == "TryParse" && method.GetParameters().Length == 4 && method.ReturnType == typeof(bool)
                                              let m_params = method.GetParameters()
                                              where m_params[0].ParameterType == typeof(String) && m_params[1].ParameterType == typeof(NumberStyles) && m_params[3].IsOut
                                              select method;

            if (methods?.Count() != 1) return false;

            String strNum = value.ToLower();
            MethodInfo TryParse = methods.First();

            if (strNum.IndexOf("0x") != -1)
            {
                style = NumberStyles.HexNumber;
                strNum = strNum.Replace("0x", "");
            }

            bool result = false;
            object[] parameters = new object[4] { strNum, style, null, (NumberType)default };

            try
            {
                result = (bool)TryParse.Invoke(null, parameters);
                if (result) number = (NumberType)parameters[3];
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
                Console.WriteLine(ex);
                return false;
            }

            return result;
        }

        /// <summary>
        /// To Number Array
        /// </summary>
        /// <typeparam name="NumberType"></typeparam>
        /// <param name="value"></param>
        /// <param name="defaultArray"></param>
        /// <param name="separator"></param>
        /// <param name="style"></param>
        public static bool TryParse<NumberType>(this String value, ref NumberType[] defaultArray, char separator = ',', NumberStyles style = NumberStyles.None)
            where NumberType : struct, IComparable, IFormattable, IConvertible, IComparable<NumberType>, IEquatable<NumberType>
        {
            if (String.IsNullOrWhiteSpace(value)) return false;

            string[] stringArray = value.Trim().Split(new char[] { separator }, StringSplitOptions.None);

            if (defaultArray == null || defaultArray.Length <= 0)
                defaultArray = new NumberType[stringArray.Length];
            int length = Math.Min(stringArray.Length, defaultArray.Length);

            IEnumerable<MethodInfo> methods = from method in typeof(NumberType).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                              where method.Name == "TryParse" && method.GetParameters().Length == 4 && method.ReturnType == typeof(bool)
                                              let m_params = method.GetParameters()
                                              where m_params[0].ParameterType == typeof(String) && m_params[1].ParameterType == typeof(NumberStyles) && m_params[3].IsOut
                                              select method;

            if (methods?.Count() != 1) return false;

            String strNum;
            object[] parameters = new object[4];
            MethodInfo TryParse = methods.First();

            for (int i = 0; i < length; i++)
            {
                if (String.IsNullOrWhiteSpace(stringArray[i])) continue;

                strNum = stringArray[i].ToLower();
                if (strNum.IndexOf("0x") != -1)
                {
                    style = NumberStyles.HexNumber;
                    strNum = strNum.Replace("0x", "");
                }

                parameters[0] = strNum;
                parameters[1] = style;
                parameters[2] = null;
                parameters[3] = (NumberType)default;

                try
                {
                    bool result = (bool)TryParse.Invoke(null, parameters);
                    if (result) defaultArray[i] = (NumberType)parameters[3];
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                    Console.WriteLine(ex);
                }
            }
            return true;
        }

        /// <summary>
        /// 将字符解析为 <see cref="System.Byte"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array">如果为空，则字符分割的数组长度返回转换结果，如果不为空，则按 array 的长度返回转换结果</param>
        /// <param name="separator">分隔字符串中子字符串的字符数组、不包含分隔符的空数组或 null。</param>
        /// <param name="style">枚举值的按位组合，用于指示可出现在 string 中的样式元素。要指定的一个典型值为 System.Globalization.NumberStyles.Integer。</param>
        /// <param name="provider">一个对象，提供有关 string 的区域性特定格式设置信息。</param>
        /// <returns></returns>
        public static bool ToByteArray(this String value, ref byte[] array, char separator = ',', NumberStyles style = NumberStyles.HexNumber) => TryParse<Byte>(value, ref array, separator, style);

        /// <summary>
        /// 将字符解析为 <see cref="System.Int16"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array">如果为空，则字符分割的数组长度返回转换结果，如果不为空，则按 array 的长度返回转换结果</param>
        /// <param name="separator">分隔字符串中子字符串的字符数组、不包含分隔符的空数组或 null。</param>
        /// <param name="style">枚举值的按位组合，用于指示可出现在 string 中的样式元素。要指定的一个典型值为 System.Globalization.NumberStyles.Integer。</param>
        /// <returns></returns>
        public static bool ToInt16Array(this String value, ref Int16[] array, char separator = ',', NumberStyles style = NumberStyles.None) => TryParse<Int16>(value, ref array, separator, style);
        /// <summary>
        /// 将字符解析为 <see cref="System.UInt16"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array">如果为空，则字符分割的数组长度返回转换结果，如果不为空，则按 array 的长度返回转换结果</param>
        /// <param name="separator">分隔字符串中子字符串的字符数组、不包含分隔符的空数组或 null。</param>
        /// <param name="style">枚举值的按位组合，用于指示可出现在 string 中的样式元素。要指定的一个典型值为 System.Globalization.NumberStyles.Integer。</param>
        /// <returns></returns>
        public static bool ToUInt16Array(this String value, ref UInt16[] array, char separator = ',', NumberStyles style = NumberStyles.HexNumber) => TryParse<UInt16>(value, ref array, separator, style);

        /// <summary>
        /// 将字符解析为 <see cref="System.Int32"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array">如果为空，则按字符串分割的数组长度返回转换结果，如果不为空，则按 array 的长度返回转换结果；</param>
        /// <param name="separator">分隔字符串中子字符串的字符数组、不包含分隔符的空数组或 null。</param>
        /// <param name="style">枚举值的按位组合，用于指示可出现在 string 中的样式元素。要指定的一个典型值为 System.Globalization.NumberStyles.Integer。</param>
        /// <param name="provider">一个对象，提供有关 string 的区域性特定格式设置信息。</param>
        /// <returns></returns>
        public static bool ToInt32Array(this String value, ref int[] array, char separator = ',', NumberStyles style = NumberStyles.None) => TryParse<Int32>(value, ref array, separator, style);
        /// <summary>
        /// 将字符解析为 <see cref="System.UInt32"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array"></param>
        /// <param name="separator"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public static bool ToUInt32Array(this String value, ref uint[] array, char separator = ',', NumberStyles style = NumberStyles.HexNumber) => TryParse<UInt32>(value, ref array, separator, style);

        /// <summary>
        /// 将字符串参数集，分割转换为字符串数组，注意：不支持中文字符串
        /// <para>示例字符串："0x01,True,32,False"，输出数组：["0x01","True","32","False"]</para>
        /// <para>示例字符串："0x01,3,[True,True,False]"，输出数组：["0x01","3",["True","True","False"]]</para>
        /// <para>示例字符串："0x01,[0,3,4,7],[True,True,False,True]"，输出数组：["0x01",["0","3","4","7"],["True","True","False","True"]]</para>
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object[] SplitParameters(String parameters)
        {
            if (String.IsNullOrWhiteSpace(parameters)) return null;

            String pattern = @"\[([\w\s,]+)\]|([\w\s]+),|([\w]+)";
            MatchCollection matchs = Regex.Matches(parameters, pattern, RegexOptions.Compiled | RegexOptions.Singleline);

            List<object> args = new List<object>();
            foreach (Match match in matchs)
            {
                String value = match.Value;
                //Console.WriteLine($"Match:: {match.Success}({match.Groups.Count}) {match.Value}");

                foreach (Group group in match.Groups)
                {
                    if (group.Success && group.Value != match.Value)
                    {
                        value = group.Value;
                        //Console.WriteLine($"\t-{group.Value}-");
                        break;
                    }
                }

                //Console.WriteLine($"NewValue::{value}");
                if (value.IndexOf(',') != -1)
                    args.Add(value.Split(','));
                else
                    args.Add(value);
            }

            return args.ToArray();
        }

        /// <summary>
        /// 数组类型参数转换
        /// <para>示例：var array = StringExtension.ConvertParamsToArrayType(typeof(Byte[]), new Object[] { "0x45", "0x46", 0x47 });</para>
        /// </summary>
        /// <param name="paramArrayType"></param>
        /// <param name="paramValues"></param>
        /// <returns></returns>
        public static System.Array ConvertParamsToArrayType(Type paramArrayType, object[] paramValues)
        {
            if (paramArrayType == null || paramValues?.Length <= 0) return null;
            if (!paramArrayType.IsArray) throw new ArgumentException(nameof(paramArrayType), "参数应为数组或集合类型数据");

            Type elementType = paramArrayType.GetElementType();
            Array arguments = Array.CreateInstance(elementType, paramValues.Length);

            for (int i = 0; i < paramValues.Length; i++)
                arguments.SetValue(ConvertParamsToValueType(elementType, paramValues[i]), i);

            return arguments;
        }

        /// <summary>
        /// 值类型参数转换
        /// <para>示例：UInt32 value = (UInt32)StringExtension.ConvertParamsToValueType(typeof(UInt32), "45");</para>
        /// <para>示例：var value = StringExtension.ConvertParamsToValueType(typeof(Byte), "0x45"); 或 var value = StringExtension.ConvertParamsToValueType(typeof(Byte), 0x45);</para>
        /// </summary>
        /// <param name="paramType">参数类型</param>
        /// <param name="paramValue">参数值，或值字符串</param>
        /// <returns></returns>
        public static System.ValueType ConvertParamsToValueType(Type paramType, object paramValue)
        {
            if (paramType == null) return null;
            if (paramValue == null || String.IsNullOrWhiteSpace(paramValue.ToString()) || paramValue.ToString().ToLower().Trim() == "null") return null;
            if (!paramType.IsValueType) throw new ArgumentException(nameof(paramType), "参数类型应为值类型参数");

            if (paramType.GetType() == paramType) return (ValueType)paramValue;
            if (paramValue.GetType() != typeof(String)) return (ValueType)Convert.ChangeType(paramValue, paramType);

            if (paramType.IsEnum)
            {
                return (ValueType)Enum.Parse(paramType, paramValue.ToString(), true);
            }
            else if (paramType == typeof(bool))
            {
                return bool.TryParse(paramValue.ToString(), out bool value) && value;
            }
            else if (paramType == typeof(sbyte) || paramType == typeof(byte) || paramType == typeof(short) || paramType == typeof(ushort) ||
                paramType == typeof(Int32) || paramType == typeof(UInt32) || paramType == typeof(Int64) || paramType == typeof(UInt64))
            {
                IEnumerable<MethodInfo> methods = from method in paramType.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                  where method.Name == "TryParse" && method.GetParameters().Length == 4 && method.ReturnType == typeof(bool)
                                                  let m_params = method.GetParameters()
                                                  where m_params[0].ParameterType == typeof(String) && m_params[1].ParameterType == typeof(NumberStyles) && m_params[3].IsOut
                                                  select method;

                if (methods?.Count() != 1) return (ValueType)paramValue;

                MethodInfo TryParse = methods.First();
                NumberStyles style = NumberStyles.None;
                String strNum = paramValue.ToString().ToLower();

                if (strNum.IndexOf("0x") != -1)
                {
                    style = NumberStyles.HexNumber;
                    strNum = strNum.Replace("0x", "");
                }

                object[] parameters = new object[4] { strNum, style, null, Activator.CreateInstance(paramType) };

                try
                {
                    bool result = (bool)TryParse.Invoke(null, parameters);
                    return (ValueType)parameters[3];
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                    Console.WriteLine(ex);
                    return (ValueType)parameters[3];
                }
            }
            else
            {
                Logger.Warn($"暂不支持的类型转换 {paramType},{paramValue}");
            }

            return (ValueType)Convert.ChangeType(paramValue, paramType);
        }
    }
}
