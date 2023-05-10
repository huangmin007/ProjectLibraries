using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SpaceCG.Extensions
{
    public static partial class StringExtensions
    {
        /// <summary>
        /// log4net.Logger 对象
        /// </summary>
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(nameof(StringExtensions));

        private static object[] GetNumberStringBase(String numberString)
        {
            if (String.IsNullOrWhiteSpace(numberString)) throw new ArgumentNullException(nameof(numberString), "参数不能为空");

            object[] parameters = new object[2];
            String numString = numberString.ToUpper().Replace(" ", "").Replace("_", "");

            if (numString.IndexOf("0B") == 0)
            {
                parameters[1] = 2;
                parameters[0] = numString.Substring(2);
            }
            else if (numString.IndexOf("O") == 0)
            {
                parameters[1] = 8;
                parameters[0] = numString.Substring(1);
            }
            else if (numString.IndexOf("0D") == 0)
            {
                parameters[1] = 10;
                parameters[0] = numString.Substring(2);
            }
            else if (numString.IndexOf("0X") == 0)
            {
                parameters[1] = 16;
                parameters[0] = numString.Substring(2);
            }
            else
            {
                parameters[1] = 10;
                parameters[0] = numString;
            }

            return parameters;
        }

        /// <summary>
        /// 将数字字符类型转为数值类型，支持二进制(0B)、八进制(O)、十进制(0D)、十六进制(0X)字符串的转换。
        /// </summary>
        /// <typeparam name="NumberType"></typeparam>
        /// <param name="numberString"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryParse<NumberType>(this String numberString, out NumberType result) 
            where NumberType : struct, IComparable, IFormattable, IConvertible, IComparable<NumberType>, IEquatable<NumberType>
        {
            result = default;
            if (String.IsNullOrWhiteSpace(numberString)) return false;

            String methodName = $"To{typeof(NumberType).Name}";
            IEnumerable<MethodInfo> methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                              where method.Name == methodName && method.GetParameters().Length == 2 && method.ReturnType == typeof(NumberType)
                                              let m_params = method.GetParameters()
                                              where m_params[0].ParameterType == typeof(String) && m_params[1].ParameterType == typeof(int)
                                              select method;

            if (methods?.Count() != 1) return false;

            MethodInfo ConvertToNumber = methods.First();
            object[] parameters = GetNumberStringBase(numberString);

            try
            {
                result = (NumberType)ConvertToNumber.Invoke(null, parameters);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 将多个数值字符类型(默认以 ',' 分割)型转为数值数组类型，支持二进制(0B)、八进制(O)、十进制(0D)、十六进制(0X)字符串的转换。
        /// </summary>
        /// <typeparam name="NumberType"></typeparam>
        /// <param name="numberString"></param>
        /// <param name="defaultValues"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static bool TryParse<NumberType>(this String numberString, ref NumberType[] defaultValues, char separator = ',')
            where NumberType : struct, IComparable, IFormattable, IConvertible, IComparable<NumberType>, IEquatable<NumberType>
        {
            if (String.IsNullOrWhiteSpace(numberString)) return false;

            string[] stringArray = numberString.Trim().Split(new char[] { separator }, StringSplitOptions.None);

            if (defaultValues == null || defaultValues.Length <= 0)
                defaultValues = new NumberType[stringArray.Length];
            int length = Math.Min(stringArray.Length, defaultValues.Length);

            String methodName = $"To{typeof(NumberType).Name}";
            IEnumerable<MethodInfo> methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                              where method.Name == methodName && method.GetParameters().Length == 2 && method.ReturnType == typeof(NumberType)
                                              let m_params = method.GetParameters()
                                              where m_params[0].ParameterType == typeof(String) && m_params[1].ParameterType == typeof(int)
                                              select method;

            if (methods?.Count() != 1) return false;

            MethodInfo ConvertToNumber = methods.First();

            for (int i = 0; i < length; i++)
            {
                if (String.IsNullOrWhiteSpace(stringArray[i])) continue;

                NumberType newValue;
                object[] parameters = GetNumberStringBase(numberString);

                try
                {
                    newValue = (NumberType)ConvertToNumber.Invoke(null, parameters);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                    continue;
                }

                defaultValues[i] = newValue;
            }

            return true;
        }


#if false
        /// <summary>
        /// 将数字字符类型型转为数值类型
        /// </summary>
        /// <typeparam name="NumberType"></typeparam>
        /// <param name="value"></param>
        /// <param name="number"></param>
        /// <param name="style"></param>
        [Obsolete("不在使用", true)]
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

            MethodInfo TryParse = methods.First();
            String strNum = value.ToLower().Trim();

            if (strNum.IndexOf("0x") == 0)
            {
                style = NumberStyles.HexNumber;
                strNum = strNum.Replace("0x", "");
            }
            else if(strNum.IndexOf("0b") == 0)
            {

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
        [Obsolete("不在使用", true)]
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
#endif


        /// <summary>
        /// 将多个 <see cref="System.SByte"/> 格式的字符串解析为 <see cref="System.SByte"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static bool TryParseToSByteArray(this String value, ref sbyte[] array, char separator = ',') => TryParse<SByte>(value, ref array, separator);
        /// <summary>
        /// 将多个 <see cref="System.Byte"/> 格式的字符串解析为 <see cref="System.Byte"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static bool TryParseToByteArray(this String value, ref byte[] array, char separator = ',') => TryParse<Byte>(value, ref array, separator);

        /// <summary>
        /// 将多个 <see cref="System.Int16"/> 格式的字符串解析为 <see cref="System.Int16"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static bool TryParseToInt16Array(this String value, ref Int16[] array, char separator = ',') => TryParse<Int16>(value, ref array, separator);
        /// <summary>
        /// 将多个 <see cref="System.UInt16"/> 格式的字符串解析为 <see cref="System.UInt16"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static bool TryParseToUInt16Array(this String value, ref UInt16[] array, char separator = ',') => TryParse<UInt16>(value, ref array, separator);

        /// <summary>
        /// 将多个 <see cref="System.Int32"/> 格式的字符串解析为 <see cref="System.Int32"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static bool TryParseToInt32Array(this String value, ref Int32[] array, char separator = ',') => TryParse<Int32>(value, ref array, separator);
        /// <summary>
        /// 将多个 <see cref="System.UInt32"/> 格式的字符串解析为 <see cref="System.UInt32"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static bool TryParseToUInt32Array(this String value, ref UInt32[] array, char separator = ',') => TryParse<UInt32>(value, ref array, separator);


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
            if (String.IsNullOrWhiteSpace(parameters)) return new object[] { }; // null

            //Console.WriteLine(parameters);
            //Regex regex = new Regex(@"\w\a");

            String pattern = @"\[([\w\s\#\.,]+)\]|([\w\s\#\.]+),|([\w\#\.]+)";
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
        /// 数组类型参数转换
        /// <para>示例：var array = StringExtension.ConvertParamsToArrayType(typeof(Byte[]), new Object[] { "0x45", "0x46", 0x47 });</para>
        /// </summary>
        /// <param name="paramArrayType"></param>
        /// <param name="paramValues"></param>
        /// <returns></returns>
        public static System.Array ConvertParamsToArrayType(Type paramArrayType, Array paramValues)
        {
            if (paramArrayType == null || paramValues?.Length <= 0) return null;
            if (!paramArrayType.IsArray) throw new ArgumentException(nameof(paramArrayType), "参数应为数组或集合类型数据");

            Type elementType = paramArrayType.GetElementType();
            Array arguments = Array.CreateInstance(elementType, paramValues.Length);

            for (int i = 0; i < paramValues.Length; i++)
                arguments.SetValue(ConvertParamsToValueType(elementType, paramValues.GetValue(i)), i);

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
                if (bool.TryParse(paramValue.ToString(), out bool value)) return value;
                String pv = paramValue.ToString().Replace(" ", "");
                return pv == "1" || pv == "T";
            }
            else if (paramType == typeof(sbyte) || paramType == typeof(byte) || paramType == typeof(short) || paramType == typeof(ushort) ||
                paramType == typeof(Int32) || paramType == typeof(UInt32) || paramType == typeof(Int64) || paramType == typeof(UInt64))
            {
                String methodName = $"To{paramType.Name}";
                IEnumerable<MethodInfo> methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                  where method.Name == methodName && method.GetParameters().Length == 2 && method.ReturnType == paramType
                                                  let m_params = method.GetParameters()
                                                  where m_params[0].ParameterType == typeof(String) && m_params[1].ParameterType == typeof(int)
                                                  select method;

                if (methods?.Count() != 1) return (ValueType)paramValue;

                MethodInfo ConvertToNumber = methods.First();
                object[] parameters = GetNumberStringBase(paramValue.ToString());

                try
                {
                    return (ValueType)ConvertToNumber.Invoke(null, parameters);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                    return (ValueType)paramValue;
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
