using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SpaceCG.Generic;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// StringExtensions
    /// </summary>
    public static partial class StringExtensions
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(StringExtensions));

        /// <summary>
        /// 获取数值型的字符串进位基数
        /// <para>可解析示例：十六进制字符串 "0x45", 二进制字符串 "0B1101_1101", 八进制字符串 "O12", 十进制字符串 "0D12.5"或"12.5"</para>
        /// </summary>
        /// <param name="numberString"></param>
        /// <returns></returns>
        internal static object[] GetNumberStringBase(String numberString)
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

            IEnumerable<MethodInfo> methods;
            String methodName = $"To{typeof(NumberType).Name}";
            if (typeof(NumberType) == typeof(double) || typeof(NumberType) == typeof(float))
            {
                methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                          where method.Name == methodName && method.GetParameters().Length == 1 && method.ReturnType == typeof(NumberType)
                          let m_params = method.GetParameters()
                          where m_params[0].ParameterType == typeof(String)
                          select method;
            }
            else
            {
                methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                          where method.Name == methodName && method.GetParameters().Length == 2 && method.ReturnType == typeof(NumberType)
                          let m_params = method.GetParameters()
                          where m_params[0].ParameterType == typeof(String) && m_params[1].ParameterType == typeof(int)
                          select method;
            }

            if (methods?.Count() != 1) return false;
            MethodInfo ConvertToNumber = methods.First();
            numberString = numberString.ToUpper().Replace(" ", "").Replace("_", "");
            object[] parameters = ConvertToNumber.GetParameters().Length == 1 ? new object[] { numberString } : GetNumberStringBase(numberString);

            try
            {
                result = (NumberType)ConvertToNumber.Invoke(null, parameters);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message} {numberString}/{typeof(NumberType)}");
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

            IEnumerable<MethodInfo> methods;
            String methodName = $"To{typeof(NumberType).Name}";
            if (typeof(NumberType) == typeof(double) || typeof(NumberType) == typeof(float))
            {
                methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                          where method.Name == methodName && method.GetParameters().Length == 1 && method.ReturnType == typeof(NumberType)
                          let m_params = method.GetParameters()
                          where m_params[0].ParameterType == typeof(String)
                          select method;
            }
            else
            {
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
            ParameterInfo[] ParamsInfo = ConvertToNumber.GetParameters();
            int length = Math.Min(stringArray.Length, defaultValues.Length);

            for (int i = 0; i < length; i++)
            {
                if (String.IsNullOrWhiteSpace(stringArray[i])) continue;

                NumberType newValue = default;
                object[] parameters = ParamsInfo.Length == 1 ? new object[] { stringArray[i] } : GetNumberStringBase(stringArray[i]);

                try
                {
                    newValue = (NumberType)ConvertToNumber.Invoke(null, parameters);
                }
                catch (Exception ex)
                {
                    Logger.Error($"{ex.Message} {stringArray[i]}/{typeof(NumberType)}");
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

        /// <summary> 正则匹配 '~' | "~" </summary>
        private static readonly String pattern_string = @"\'([^\']+)\'|" + "\"([^\"]+)\"";
        /// <summary> 正则匹配 [~] </summary>
        private static readonly String pattern_array = @"\[([^\[\]]+)\]";
#pragma warning disable CS0414
        /// <summary> 正则匹配 (~) </summary>
        private static readonly String pattern_parent = @"\(([^\(\)]+)\)";
#pragma warning restore CS0414
        /// <summary> 正则匹配 ',' 分割, 或结尾部份 </summary>
        private static readonly String pattern_split = @"([^\,\'\[\]]+),|([^\,\'\[\]]+)$";
        private static readonly String pattern_arguments = $@"{pattern_string}|{pattern_array}|{pattern_split}";
        /// <summary>
        /// 字符串参数正则表达式
        /// </summary>
        public static readonly Regex RegexStringArguments = new Regex(pattern_arguments, RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// 将字符串参数集，分割转换为字符串数组
        /// <para>示例字符串："0x01,True,32,False"，输出数组：["0x01","True","32","False"]</para>
        /// <para>示例字符串："0x01,3,[True,True,False]"，输出数组：["0x01","3",["True","True","False"]]</para>
        /// <para>示例字符串："0x01,[0,3,4,7],[True,True,False,True]"，输出数组：["0x01",["0","3","4","7"],["True","True","False","True"]]</para>
        /// <para>示例字符串：" 'hello,world',0x01,3,'ni?,hao,[aa,bb]', [True,True,False],['aaa,bb,c','ni,hao'],15,\"aa,aaa\",15"，输出数组：["hello,world","0x01","3","ni?,hao,[aa,bb]",["True","True","False","True"],["aaa,bb,c","ni,hao"],"15","aa,aaa","15"]</para>
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object[] SplitParameters(string parameters)
        {
            if (String.IsNullOrWhiteSpace(parameters)) return new object[] { }; // null

#if false
            String pattern_string   = @"\'([^\']+)\'|" + "\"([^\"]+)\"";    //匹配'~',"~"
            String pattern_array    = @"\[([^\[\]]+)\]";                    //匹配[~]
            String pattern_parent   = @"\(([^\(\)]+)\)";                    //匹配(~)
            String pattern_split    = @"([^\,\'\[\]]+),|([^\,\'\[\]]+)$";   //匹配 ',' 分割, 或结尾部份
            String pattern = $@"{pattern_string}|{pattern_array}|{pattern_split}";
            MatchCollection matchs = Regex.Matches(parameters, pattern, RegexOptions.Compiled | RegexOptions.Singleline);
#else
            MatchCollection matchs = RegexStringArguments.Matches(parameters);
#endif

            List<object> args = new List<object>();
            foreach (Match match in matchs)
            {
                if (!match.Success) continue;
#if true
                String trimValue = match.Value.Trim();
                if((trimValue.IndexOf('\'') == 0 && trimValue.LastIndexOf('\'') == trimValue.Length - 1) ||
                   (trimValue.IndexOf('\"') == 0 && trimValue.LastIndexOf('\"') == trimValue.Length - 1))
                {
                    args.Add(trimValue.Substring(1, trimValue.Length - 2));
                }
                else if(trimValue.IndexOf('[') == 0 && trimValue.LastIndexOf(']') == trimValue.Length - 1)
                {
                    args.Add(SplitParameters(trimValue.Substring(1, trimValue.Length - 2)));
                }
                else if(trimValue.LastIndexOf(',') == trimValue.Length - 1)
                {
                    args.Add(trimValue.Substring(0, trimValue.Length - 1));
                }
                else
                {
                    args.Add(match.Value);
                }
#else
                //.Net Framework 4.7 或以上版本  
                //.NET Standard 2.1 或以上版本
                //.NET 5,6,7
                foreach (Group group in match.Groups)
                {
                    if (group.Success && match.Name != group.Name)
                    {
                        if (match.Name != "3") //[~]
                            args.Add(group.Value);
                        else
                            args.Add(SplitParameters(group.Value));
                    }
                }
#endif
            }

            return args.ToArray();
        }

        /// <summary>
        /// 数组类型参数转换
        /// <para>输出一个指定类型的对象, 该对象的值等效于指定的对象</para>
        /// <para>示例：StringExtensions.ConvertChangeTypeToArrayType(new object[] { "0x45", "0x46", 0x47 }, typeof(Byte[]), out Array bytes);</para>
        /// </summary>
        /// <param name="value">需要转换的对象、字符串或字符串描述</param>
        /// <param name="conversionType">要返回的对象的类型</param>
        /// <param name="conversionValue">返回一个对象，其类型为 conversionType，并且其值等效于 value </param>
        /// <returns>输出类型的值 conversionValue 为有效对象返回 true, 否则返回 false </returns>
        /// <exception cref="ArgumentException"/>
        public static bool ConvertChangeTypeToArrayType(Array value, Type conversionType, out Array conversionValue)
        {
            conversionValue = null;
            if (conversionType == null || !conversionType.IsArray || !conversionType.GetElementType().IsValueType)
                throw new ArgumentException(nameof(conversionType), "需要转换的类型应为数组类型，且数组元素为值类型");

            if (value == null) return true;
            if (value.GetType() == typeof(string))
            {
                string valueString = value.ToString();
                if (string.IsNullOrWhiteSpace(valueString) || valueString.ToLower().Trim() == "null") return true;
            }

            if (!value.GetType().IsArray) throw new ArgumentException(nameof(value), "需要转换的值对象也应该为数组类型");

            Type elementType = conversionType.GetElementType();
            conversionValue = Array.CreateInstance(elementType, value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                if(ConvertChangeTypeToValueType(value.GetValue(i), elementType, out ValueType cValue))
                {
                    conversionValue.SetValue(cValue, i);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 值类型参数转换
        /// <para>输出一个指定类型的对象, 该对象的值等效于指定的对象</para>
        /// <para>示例：StringExtensions.ConvertChangeTypeToValueType("45", typeof(UInt32), out UInt32 cValue);</para>
        /// </summary>
        /// <param name="value">需要转换的对象、字符串或字符串描述</param>
        /// <param name="conversionType">要返回的对象的类型</param>
        /// <param name="conversionValue">返回一个对象，其类型为 conversionType，并且其值等效于 value </param>
        /// <returns>输出类型的值 conversionValue 为有效对象返回 true, 否则返回 false </returns>
        /// <exception cref="ArgumentException"/>
        public static bool ConvertChangeTypeToValueType(object value, Type conversionType, out ValueType conversionValue)
        {
            conversionValue = null;
            if (conversionType == null || !conversionType.IsValueType) 
                throw new ArgumentException(nameof(conversionType), "需要转换的类型应为值类型参数");

            if (value == null) return true;
            if (value.GetType() == typeof(string))
            {
                string valueString = value.ToString();
                if (string.IsNullOrWhiteSpace(valueString) || valueString.ToLower().Trim() == "null") return true;
            }

            if (value.GetType() == conversionType)
            {
                conversionValue = value as ValueType;
                return true;
            }
            
            //Enum
            if (conversionType.IsEnum)
            {
                conversionValue = Enum.Parse(conversionType, value.ToString(), true) as ValueType;
                return true;
            }
            //Boolean
            else if (conversionType == typeof(bool))
            {
                if (bool.TryParse(value.ToString(), out bool result))
                {
                    conversionValue = result;
                    return true;
                }
                string pv = value.ToString().Replace(" ", "");
                conversionValue = pv == "1" || pv == "T";
                return true;
            }
            //Number
            else if (conversionType == typeof(sbyte) || conversionType == typeof(byte) || conversionType == typeof(short) || conversionType == typeof(ushort) ||
                conversionType == typeof(Int32) || conversionType == typeof(UInt32) || conversionType == typeof(Int64) || conversionType == typeof(UInt64))
            {
                string methodName = $"To{conversionType.Name}";                
                IEnumerable<MethodInfo> methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                  where method.Name == methodName && method.GetParameters().Length == 2 && method.ReturnType == conversionType
                                                  let m_params = method.GetParameters()
                                                  where m_params[0].ParameterType == typeof(string) && m_params[1].ParameterType == typeof(int)
                                                  select method;

                conversionValue = value as ValueType;
                if (methods?.Count() != 1) return false;
                
                MethodInfo ConvertToNumber = methods.First(); 
                object[] parameters = GetNumberStringBase(value.ToString());

                try
                {
                    conversionValue = ConvertToNumber.Invoke(null, parameters) as ValueType;
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString());
                    return false;
                }
            }
            //Double,Float
            else if(conversionType == typeof(double) || conversionType == typeof(float))
            {
                string methodName = $"To{conversionType.Name}";
                IEnumerable<MethodInfo> methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                                    where method.Name == methodName && method.GetParameters().Length == 1 && method.ReturnType == conversionType
                                                    let m_params = method.GetParameters()
                                                    where m_params[0].ParameterType == typeof(string)
                                                    select method;

                conversionValue = value as ValueType;
                if (methods?.Count() != 1) return false;

                MethodInfo ConvertToNumber = methods.First();
                object[] parameters = new object[] { value.ToString().Replace(" ", "").Replace("_", "") };

                try
                {
                    conversionValue = ConvertToNumber.Invoke(null, parameters) as ValueType;
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString());
                    return false;
                }
            }
            //Other
            else
            {
                try
                {
                    if (typeof(IConvertible).IsAssignableFrom(value.GetType()))
                    {
                        conversionValue = Convert.ChangeType(value, conversionType) as ValueType;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"值类型转换失败  Value:{value}  Type:{conversionType}");
                    Logger.Error(ex);
                }

                return false;
            }
        }

    }
}
