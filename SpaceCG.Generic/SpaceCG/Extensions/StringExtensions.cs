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
        internal static object[] GetNumberStringBase(string numberString)
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
            else if(numberString.IndexOf('.') != -1)
            {
                return new object[] { numberString };
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
        /// <param name="number"></param>
        /// <returns></returns>
        public static bool ToNumber<NumberType>(this string numberString, out NumberType number)
            where NumberType : struct, IComparable, IFormattable, IConvertible, IComparable<NumberType>, IEquatable<NumberType>
        {
            number = default;
            if (string.IsNullOrWhiteSpace(numberString)) return false;
            Type numberType = typeof(NumberType);
            if (!TypeExtensions.IsNumeric(numberType)) throw new ArgumentException(nameof(numberType), "类型错误");

            Type intType = typeof(int);
            Type stringType = typeof(string);
            string methodName = $"To{numberType.Name}";
            int paramsLength = TypeExtensions.IsFloat(numberType) ? 1 : 2;

            IEnumerable<MethodInfo> methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                              let m_params = method.GetParameters()
                                              where method.Name == methodName && m_params.Length == paramsLength && method.ReturnType == numberType
                                              where (paramsLength == 1 && m_params[0].ParameterType == stringType) ||
                                                    (paramsLength == 2 && m_params[0].ParameterType == stringType && m_params[1].ParameterType == intType)
                                              select method;

            if (methods?.Count() != 1) return false;
            MethodInfo ConvertToNumber = methods.First();
            object[] parameters = GetNumberStringBase(numberString);

            try
            {
                number = (NumberType)ConvertToNumber.Invoke(null, parameters);
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message} {numberString}/{numberType}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 将数字字符类型转为数值类型，支持二进制(0B)、八进制(O)、十进制(0D)、十六进制(0X)、Double、Float 字符串的转换。
        /// </summary>
        /// <param name="numberString"></param>
        /// <param name="numberType"></param>
        /// <param name="number"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static bool ToNumber(this string numberString, Type numberType, out ValueType number)
        {
            number = 0;
            if (string.IsNullOrWhiteSpace(numberString)) return false;
            if (!TypeExtensions.IsNumeric(numberType)) throw new ArgumentException(nameof(numberType), "类型错误");

            Type intType = typeof(int);
            Type stringType = typeof(string);
            string methodName = $"To{numberType.Name}";
            int paramsLength = TypeExtensions.IsFloat(numberType) ? 1 : 2;

            IEnumerable<MethodInfo> methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                              let m_params = method.GetParameters()
                                              where method.Name == methodName && m_params.Length == paramsLength && method.ReturnType == numberType
                                              where (paramsLength == 1 && m_params[0].ParameterType == stringType) ||
                                                    (paramsLength == 2 && m_params[0].ParameterType == stringType && m_params[1].ParameterType == intType)
                                              select method;

            if (methods?.Count() != 1) return false;
            MethodInfo ConvertToNumber = methods.First();
            object[] parameters = GetNumberStringBase(numberString);

            try
            {
                number = (ValueType)ConvertToNumber.Invoke(null, parameters);
            }
            catch (Exception ex)
            {
                Logger.Warn($"{ex.Message} {numberString}/{numberType}");
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
        public static bool ToNumberArray<NumberType>(this string numberString, ref NumberType[] defaultValues, char separator = ',')
            where NumberType : struct, IComparable, IFormattable, IConvertible, IComparable<NumberType>, IEquatable<NumberType>
        {
            if (string.IsNullOrWhiteSpace(numberString)) return false;

            Type intType = typeof(int);
            Type stringType = typeof(string);
            Type numberType = typeof(NumberType);
            string methodName = $"To{numberType.Name}";
            int paramsLength = TypeExtensions.IsFloat(numberType) ? 1 : 2;
            IEnumerable<MethodInfo> methods = from method in typeof(Convert).GetMethods(BindingFlags.Static | BindingFlags.Public)
                                              let m_params = method.GetParameters()
                                              where method.Name == methodName && m_params.Length == paramsLength && method.ReturnType == numberType
                                              where (paramsLength == 1 && m_params[0].ParameterType == stringType) ||
                                                    (paramsLength == 2 && m_params[0].ParameterType == stringType && m_params[1].ParameterType == intType)
                                              select method;

            if (methods?.Count() != 1) return false;
            numberString = numberString.ToUpper().Replace(" ", "").Replace("_", "");

            string[] stringArray = numberString.Split(new char[] { separator }, StringSplitOptions.None);
            if (defaultValues == null || defaultValues.Length <= 0) defaultValues = new NumberType[stringArray.Length];

            MethodInfo ConvertToNumber = methods.First();
            ParameterInfo[] ParamsInfo = ConvertToNumber.GetParameters();
            int length = Math.Min(stringArray.Length, defaultValues.Length);

            for (int i = 0; i < length; i++)
            {
                if (string.IsNullOrWhiteSpace(stringArray[i])) continue;

                NumberType newValue = default;
                object[] parameters = GetNumberStringBase(stringArray[i]);

                try
                {
                    newValue = (NumberType)ConvertToNumber.Invoke(null, parameters);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"{ex.Message} {stringArray[i]}/{numberType}");
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
        public static bool ToSByteArray(this string value, ref sbyte[] array, char separator = ',') => ToNumberArray<SByte>(value, ref array, separator);
        /// <summary>
        /// 将多个 <see cref="System.Byte"/> 格式的字符串解析为 <see cref="System.Byte"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static bool ToByteArray(this string value, ref byte[] array, char separator = ',') => ToNumberArray<Byte>(value, ref array, separator);

        /// <summary>
        /// 将多个 <see cref="System.Int16"/> 格式的字符串解析为 <see cref="System.Int16"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static bool ToInt16Array(this string value, ref Int16[] array, char separator = ',') => ToNumberArray<Int16>(value, ref array, separator);
        /// <summary>
        /// 将多个 <see cref="System.UInt16"/> 格式的字符串解析为 <see cref="System.UInt16"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static bool ToUInt16Array(this string value, ref UInt16[] array, char separator = ',') => ToNumberArray<UInt16>(value, ref array, separator);

        /// <summary>
        /// 将多个 <see cref="System.Int32"/> 格式的字符串解析为 <see cref="System.Int32"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static bool ToInt32Array(this string value, ref Int32[] array, char separator = ',') => ToNumberArray<Int32>(value, ref array, separator);
        /// <summary>
        /// 将多个 <see cref="System.UInt32"/> 格式的字符串解析为 <see cref="System.UInt32"/> 类型数组
        /// </summary>
        /// <param name="value"></param>
        /// <param name="array"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static bool ToUInt32Array(this string value, ref UInt32[] array, char separator = ',') => ToNumberArray<UInt32>(value, ref array, separator);

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
        public static object[] SplitToObjectArray(this string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters)) return new object[] { }; // null
            
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
                string trimValue = match.Value.Trim();
                if ((trimValue.IndexOf('\'') == 0 && trimValue.LastIndexOf('\'') == trimValue.Length - 1) ||
                   (trimValue.IndexOf('\"') == 0 && trimValue.LastIndexOf('\"') == trimValue.Length - 1))
                {
                    args.Add(trimValue.Substring(1, trimValue.Length - 2));
                }
                else if (trimValue.IndexOf('[') == 0 && trimValue.LastIndexOf(']') == trimValue.Length - 1)
                {
                    args.Add(SplitToObjectArray(trimValue.Substring(1, trimValue.Length - 2)));
                }
                else if (trimValue.LastIndexOf(',') == trimValue.Length - 1)
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
        /// <param name="destinationType">要返回的对象的类型</param>
        /// <param name="conversionValue">返回一个对象，其类型为 conversionType，并且其值等效于 value </param>
        /// <returns>输出类型的值 conversionValue 为有效对象返回 true, 否则返回 false </returns>
        /// <exception cref="ArgumentException"/>
        [Obsolete("弃用", true)]
        public static bool ConvertChangeTypeToArrayType(Array value, Type destinationType, out Array conversionValue)
        {
            conversionValue = null;
            if (destinationType == null || !destinationType.IsArray || !destinationType.GetElementType().IsValueType)
                throw new ArgumentException(nameof(destinationType), "需要转换的类型应为数组类型，且数组元素为值类型");

            if (value == null) return true;
            if (value.GetType() == typeof(string))
            {
                string valueString = value.ToString();
                if (string.IsNullOrWhiteSpace(valueString) || valueString.ToLower().Trim() == "null") return true;
            }

            if (!value.GetType().IsArray) throw new ArgumentException(nameof(value), "需要转换的值对象也应该为数组类型");

            Type elementType = destinationType.GetElementType();
            conversionValue = Array.CreateInstance(elementType, value.Length);

            for (int i = 0; i < value.Length; i++)
            {
                if (ConvertChangeTypeToValueType(value.GetValue(i), elementType, out ValueType cValue))
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
        /// <param name="destinationType">要返回的对象的类型</param>
        /// <param name="conversionValue">返回一个对象，其类型为 conversionType，并且其值等效于 value </param>
        /// <returns>输出类型的值 conversionValue 为有效对象返回 true, 否则返回 false </returns>
        /// <exception cref="ArgumentException"/>
        [Obsolete("弃用", true)]
        public static bool ConvertChangeTypeToValueType(object value, Type destinationType, out ValueType conversionValue)
        {
            conversionValue = null;
            if (destinationType == null || !destinationType.IsValueType)
                throw new ArgumentException(nameof(destinationType), "需要转换的类型应为值类型参数");

            if (value == null) return true;
            if (value.GetType() == destinationType)
            {
                conversionValue = value as ValueType;
                return true;
            }
            if (value.GetType() == typeof(string))
            {
                string valueString = value.ToString();
                if (string.IsNullOrWhiteSpace(valueString) || valueString.ToLower().Trim() == "null") return true;
            }

            //Enum
            if (destinationType.IsEnum)
            {
                try
                {
                    conversionValue = (ValueType)Enum.Parse(destinationType, value.ToString(), true);
                }
                catch(Exception ex) 
                {
                    Logger.Warn(ex.ToString());
                    return false; 
                }
                return true;
            }
            //Boolean
            else if (destinationType == typeof(bool))
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
            else if(TypeExtensions.IsNumeric(destinationType))
            {
                return ToNumber(value.ToString(), destinationType, out conversionValue);
            }           
            //Other
            else
            {
                try
                {
                    if (typeof(IConvertible).IsAssignableFrom(value.GetType()))
                    {
                        conversionValue = Convert.ChangeType(value, destinationType) as ValueType;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"值类型转换失败  Value:{value}  Type:{destinationType}");
                    Logger.Warn(ex);
                }

                return false;
            }
        }

    }
}
