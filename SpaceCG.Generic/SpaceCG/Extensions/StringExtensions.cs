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

        /// <summary>
        /// 获取数值型的字符串进位基数
        /// <para>可解析示例：十六进制字符串 "0x45", 二进制字符串 "0B1101_1101", 八进制字符串 "O12", 十进制字符串 "0D12.5"或"12.5"</para>
        /// </summary>
        /// <param name="numberString"></param>
        /// <returns></returns>
        private static object[] GetNumberStringBase(String numberString)
        {
            if (string.IsNullOrWhiteSpace(numberString)) 
                throw new ArgumentNullException(nameof(numberString), "参数不能为空");

            object[] parameters = new object[2];
            numberString = numberString.ToUpper().Replace(" ", "").Replace("_", "");

            if (numberString.IndexOf("0B") == 0)
            {
                parameters[1] = 2;
                parameters[0] = numberString.Substring(2);
            }
            else if (numberString.IndexOf("O") == 0)
            {
                parameters[1] = 8;
                parameters[0] = numberString.Substring(1);
            }
            else if (numberString.IndexOf("0D") == 0)
            {
                parameters[1] = 10;
                parameters[0] = numberString.Substring(2);
            }
            else if (numberString.IndexOf("0X") == 0)
            {
                parameters[1] = 16;
                parameters[0] = numberString.Substring(2);
            }
            else
            {
                parameters[1] = 10;
                parameters[0] = numberString;
            }

            return parameters;
        }
        /// <summary>
        /// 将数字字符类型转为数值类型，支持二进制(0B)、八进制(O)、十进制(0D)、十六进制(0X)、Double、Float 字符串的转换。
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

            object[] parameters;
            bool isFloatType = false;
            IEnumerable<MethodInfo> methods;
            String methodName = $"To{typeof(NumberType).Name}";
            if (typeof(NumberType) == typeof(double) || typeof(NumberType) == typeof(float))
            {
                isFloatType = true;
                methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                          where method.Name == methodName && method.GetParameters().Length == 1 && method.ReturnType == typeof(NumberType)
                          let m_params = method.GetParameters()
                          where m_params[0].ParameterType == typeof(String)
                          select method;
            }
            else
            {
                isFloatType = false;
                methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                          where method.Name == methodName && method.GetParameters().Length == 2 && method.ReturnType == typeof(NumberType)
                          let m_params = method.GetParameters()
                          where m_params[0].ParameterType == typeof(String) && m_params[1].ParameterType == typeof(int)
                          select method;
            }

            if (methods?.Count() != 1) return false;
            numberString = numberString.ToUpper().Replace(" ", "").Replace("_", "");
            parameters = isFloatType ? new object[] { numberString } : GetNumberStringBase(numberString);

            try
            {
                result = (NumberType)methods.First().Invoke(null, parameters);
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message} {numberString}/{typeof(NumberType)}");
                return false;
            }

            return true;
        }
        /// <summary>
        /// 将多个数值字符类型(默认以 ',' 分割)型转为数值数组类型，支持二进制(0B)、八进制(O)、十进制(0D)、十六进制(0X)、double、float 字符串的转换。
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

            bool isFloatType = false;
            IEnumerable<MethodInfo> methods;
            String methodName = $"To{typeof(NumberType).Name}";
            if (typeof(NumberType) == typeof(double) || typeof(NumberType) == typeof(float))
            {
                isFloatType = true;
                methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                          where method.Name == methodName && method.GetParameters().Length == 1 && method.ReturnType == typeof(NumberType)
                          let m_params = method.GetParameters()
                          where m_params[0].ParameterType == typeof(String)
                          select method;
            }
            else
            {
                isFloatType = false;
                methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                          where method.Name == methodName && method.GetParameters().Length == 2 && method.ReturnType == typeof(NumberType)
                          let m_params = method.GetParameters()
                          where m_params[0].ParameterType == typeof(String) && m_params[1].ParameterType == typeof(int)
                          select method;
            }

            if (methods?.Count() != 1) return false;
            numberString = numberString.ToUpper().Replace(" ", "").Replace("_", "");

            string[] stringArray = numberString.Split(new char[] { separator }, StringSplitOptions.None);
            if (defaultValues == null || defaultValues.Length <= 0) defaultValues = new NumberType[stringArray.Length];

            MethodInfo ConvertToNumber = methods.First();
            int length = Math.Min(stringArray.Length, defaultValues.Length);

            for (int i = 0; i < length; i++)
            {
                if (String.IsNullOrWhiteSpace(stringArray[i])) continue;

                NumberType newValue = default;
                object[] parameters = isFloatType ? new object[] { stringArray[i] } : GetNumberStringBase(stringArray[i]);

                try
                {
                    newValue = (NumberType)ConvertToNumber.Invoke(null, parameters);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"{ex.Message} {stringArray[i]}/{typeof(NumberType)}");
                    continue;
                }

                defaultValues[i] = newValue;
            }

            return true;
        }

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
        /// 将字符串参数集，分割转换为字符串数组，注意：不支持中文字符串,不支持多级数组与字符串
        /// <para>示例字符串："0x01,True,32,False"，输出数组：["0x01","True","32","False"]</para>
        /// <para>示例字符串："0x01,3,[True,True,False]"，输出数组：["0x01","3",["True","True","False"]]</para>
        /// <para>示例字符串："0x01,[0,3,4,7],[True,True,False,True]"，输出数组：["0x01",["0","3","4","7"],["True","True","False","True"]]</para>
        /// <para>示例字符串：" 'hello,world',0x01,3,'ni?,hao,[aa,bb]', [True,True,False],['aaa,bb,c','ni,hao'],15,\"aa,aaa\",15"，输出数组：["hello,world","0x01","3","ni?,hao,[aa,bb]",["True","True","False","True"],['aaa,bb,c','ni,hao'],"15,"aa,aaa",15"]</para>
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object[] SplitParameters(String parameters)
        {
            if (String.IsNullOrWhiteSpace(parameters)) return new object[] { }; // null

            //String pattern = @"\[([\w\s\#\.,]+)\]|([\w\s\#\.]+),|([\w\#\.]+)";
            //String pattern = @"\'([\w\s\#\.,\[\]]+)\'|\[([\w\s\#\.,]+)\]|([\w\s\#\.]+),|([\w\#\.]+)";

            String pattern_string   = @"\'([^\']+)\'|" + "\"([^\"]+)\"";    //以'~',"~"
            String pattern_array    = @"\[([^\[\]]+)\]";                    //以[~]
            String pattern_parent   = @"\(([^\(\)]+)\)";                    //以(~)
            String pattern_split    = @"([^\,\'\[\]]+),|([^\,\'\[\]]+)$";   //以 ',' 分割, 或结尾部份
            String pattern = $@"{pattern_string}|{pattern_array}|{pattern_split}";

            MatchCollection matchs = Regex.Matches(parameters, pattern, RegexOptions.Compiled | RegexOptions.Singleline);
            //Console.WriteLine($"Match Count: {matchs.Count}");

            List<object> args = new List<object>();
            
            foreach (Match match in matchs)
            {
                //Console.WriteLine($"Match:: {match.Success}({match.Groups.Count}) {match.Captures.Count} {match.Value}");
                if (!match.Success) continue;
                String trimValue = match.Value.Trim();

                if((trimValue.IndexOf('\'') == 0 && trimValue.LastIndexOf('\'') == trimValue.Length - 1) ||
                   (trimValue.IndexOf('\"') == 0 && trimValue.LastIndexOf('\"') == trimValue.Length - 1))
                {
                    args.Add(trimValue.Substring(1, trimValue.Length - 2));
                    //Console.WriteLine(trimValue.Substring(1, trimValue.Length - 2));
                }
                else if(trimValue.IndexOf('[') == 0 && trimValue.LastIndexOf(']') == trimValue.Length - 1)
                {
                    args.Add(trimValue.Substring(1, trimValue.Length - 2).Split(','));
                    //Console.WriteLine(trimValue.Substring(1, trimValue.Length - 2));
                }
                else if(trimValue.LastIndexOf(',') == trimValue.Length - 1)
                {
                    args.Add(trimValue.Substring(0, trimValue.Length - 1));
                    //Console.WriteLine(trimValue.Substring(0, trimValue.Length - 1));
                }
                else
                {
                    args.Add(match.Value);
                }

                //Console.WriteLine($"SplitValue::{args[args.Count - 1]}");
            }

            return args.ToArray();
        }
        
        /// <summary>
        /// 数组类型参数转换
        /// <para>示例：var array = StringExtension.ConvertParamsToArrayType(typeof(Byte[]), new Object[] { "0x45", "0x46", 0x47 });</para>
        /// </summary>
        /// <param name="paramType"></param>
        /// <param name="paramValues"></param>
        /// <returns></returns>
        public static System.Array ConvertParamsToArrayType(Type paramType, Array paramValues)
        {
            if (paramType == null || paramValues?.Length <= 0) return null;
            
            if (!paramType.IsArray) throw new ArgumentException(nameof(paramType), "参数应为数组或集合类型数据");

            Type elementType = paramType.GetElementType();
            Array arguments = Array.CreateInstance(elementType, paramValues.Length);

            for (int i = 0; i < paramValues.Length; i++)
                arguments.SetValue(ConvertParamsToValueType(elementType, paramValues.GetValue(i)), i);

            return arguments;
        }
        /// <summary>
        /// 数组类型参数转换
        /// <para>示例：var array = StringExtension.ConvertParamsToArrayType(typeof(Byte[]), new Object[] { "0x45", "0x46", 0x47 });</para>
        /// </summary>
        /// <param name="paramType"></param>
        /// <param name="paramValues"></param>
        /// <returns></returns>
        public static System.Array ConvertParamsToArrayType(Type paramType, object[] paramValues) => ConvertParamsToArrayType(paramType, (Array)paramValues);
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
            if (paramType == null || paramValue == null || 
                String.IsNullOrWhiteSpace(paramValue.ToString()) || paramValue.ToString().ToLower().Trim() == "null") return null;
            
            if (!paramType.IsValueType) throw new ArgumentException(nameof(paramType), "参数类型应为值类型参数");

            if (paramValue.GetType() == paramType) return (ValueType)paramValue;
            if (paramValue.GetType() != typeof(String)) return (ValueType)Convert.ChangeType(paramValue, paramType);

            //Enum
            if (paramType.IsEnum)
            {
                return (ValueType)Enum.Parse(paramType, paramValue.ToString(), true);
            }
            //Boolean
            else if (paramType == typeof(bool))
            {
                if (bool.TryParse(paramValue.ToString(), out bool value)) return value;
                String pv = paramValue.ToString().Replace(" ", "");
                return pv == "1" || pv == "T";
            }
            //Number
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
            //Double,Float
            else if(paramType == typeof(double) || paramType == typeof(float))
            {
                String methodName = $"To{paramType.Name}";
                IEnumerable<MethodInfo> methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                    where method.Name == methodName && method.GetParameters().Length == 1 && method.ReturnType == paramType
                                                    let m_params = method.GetParameters()
                                                    where m_params[0].ParameterType == typeof(String)
                                                    select method;

                if (methods?.Count() != 1) return (ValueType)paramValue;

                MethodInfo ConvertToNumber = methods.First();
                object[] parameters = new object[] { paramValue.ToString().Replace(" ", "").Replace("_", "") };

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
            //Other
            else
            {
                //Logger.Warn($"暂未处理的类型转换 {paramType},{paramValue}");
                return (ValueType)Convert.ChangeType(paramValue, paramType);
            }
        }
    }
}
