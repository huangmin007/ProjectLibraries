# 时空DEMO远程过程调用(RPC)消息协议 v1.0

***

> RPC协议：远程过程调用(Remote Procedure Call) 或 反射程序控制(Reflection Program Control)
>
> 需求场景：DEMO 交互控制
>
> 交互特点：局域网络，数据量小，传输频率低，属于指令、控制类型数据，对消息加密没有特别要求
>
> 思考：DEMO 端应用(2C端，Flash+C#应用) 对 XML 与 JSON 消息格式的选择问题？
>
> 分析：
>
> 1.  Flash 对 XML 的支持友好，处理性能相比 JSON 要高些
>
> 2.  Flash 本地配置文件一般都是使用 XML 格式，基本不会使用 JSON 格式做配置项
>
> 3.  C# 应用(包括Unit3D, .NET应用)，支持 LINQ(语言集成查询) to XML，JSON 格式通常会使用第三方库
>
> 4.  C# 应用在项目上都使用过 XML 和 JSON 格式，但统计使用 XML 格式较多
>
> 5.  考虑 DEMO 端的综合效率问题，建议优先支持 XML 格式，后继在考虑是否实现对 JSON 格式的支持
>
> 6.  未来可扩展，增加指令列表的描述定义文件(文件中包含UI描述，层级描述，指令描述)，自动传输至各控制端，控制端跟据描述信息自动解析，生成UI、连接、控制

## XML 消息控制格式

```html
<InvokeMessage ObjectName="" MethodName="" Parameters="" Comment="">
	<Parameter Type="System.Int32">12</Parameter>
	<Parameter Type="System.String">play</Parameter>
	<Parameter Type="System.String"><![CDATA[hello,world.如果存在特殊符号字符请使用 CDATA ]]></Parameter>
	<Parameter Type="System.Byte[]">8,9,10,A,B,C</Parameter>
</InvokeMessage>
```

| <div style="width:90pt;">**节点/@属性名称**</div> | **说明**                                                                                                                                          | <div style="width:40pt;">**值类型**</div> | <div style="width:50pt;">**必要** |
| :------------------------------------------ | :---------------------------------------------------------------------------------------------------------------------------------------------- | :------------------------------------- | :------------------------------ |
| **InvokeMessages**                          | 调用多个远程方法或函数的消息集合(按顺序调用)                                                                                                                         |                                        | 否，保留                            |
| @IntervalDelay                              | 调用多个远程方法或函数执行时，每次调用等待的间隔时间；时间单位 ms, 默认为 0 ms；*++要考虑到实现问题，不能堵塞通信进程，那Flash是实现异步的问题？？++*                                                           | Int32                                  | 否，保留                            |
| **InvokeMessage**                           | 调用远程方法或函数的消息对象，单个调用消息                                                                                                                           |                                        | 是                               |
| @ObjectName                                 | 需要控制的实例或对象的名称                                                                                                                                   | String                                 | 是                               |
| @MethodName                                 | 实例或对象的方法或函数名称                                                                                                                                   | String                                 | 是                               |
| @Parameters                                 | 节点 **Parameter** 的简单形式，**优先级低于 Parameter 节点**，所有参数将按匹配的函数参数强制类型转换；<br />示例：Parameters="12,play,'hello,world.',\[0x08,0x09,0x10,0x0A,0x0B,0x0C]" | String                                 | 否                               |
| @Comment                                    | 该条控制消息说明、注释或描述信息；也可以预留未来给控制端做 Label 使用                                                                                                          | String                                 | 否，保留                            |
| **Parameter**                               | 方法或函数的参数信息，跟据方法或函数是否存在参数而定义；<br />**优先级高于 @Parameters 属性**                                                                                      |                                        | 否                               |
| @Type                                       | 参数的数据类型，如果不明确指定数据类型，则会跟据方法对应的参数强制类型转换；<br />参考：[TypeCode 枚举](https://learn.microsoft.com/zh-cn/dotnet/api/system.typecode?view=net-7.0)         | String                                 | 否                               |
| @扩展属性或节点                                    | 可在 InvokeMessage 根节点上扩展属性，或根节点之下扩展子节点(非Parameter节点)                                                                                             |                                        |                                 |

*   [x] **建议：手动输入消息编码使用 @Parameters 属性，代码封装或序列化使用 Parameter 节点；**

> #### 控制消息示例：
>
> ```html
> <InvokeMessage ObjectName="Window" MethodName="Show" /> 
> <InvokeMessage ObjectName="Window" MethodName="Close" />
> <InvokeMessage ObjectName="Demo" MethodName="GetCurrentPage" />
> <InvokeMessage ObjectName="Demo" MethodName="OpenPage" Parameters="2,EN" />
> <InvokeMessage ObjectName="Demo" MethodName="OpenPage">
> 	<Parameter Type="System.Int32">2</Parameter>
> 	<Parameter Type="System.Enum">EN</Parameter>
> </InvokeMessage>
> <InvokeMessage ObjectName="Video" MethodName="GetCurrentPosition" />
> <InvokeMessage ObjectName="Video" MethodName="Play" />
> <InvokeMessage ObjectName="Video" MethodName="Seek" Parameters="5.6"  />
> <InvokeMessage ObjectName="Video" MethodName="Seek" >
> 	<Parameter Type="System.Float">5.6</Parameter>
> </InvokeMessage>
>
> <!--同时执行多个控制消息示例-->
> <InvokeMessages IntervalDelay="100" Comment="">
> 	<!-- 1.同步打开视频文件，2.播放视频，3.将音量调整至50% -->
> 	<InvokeMessage ObjectName="Video" MethodName="Open" />
> 	<InvokeMessage ObjectName="Video" MethodName="Play" />
> 	<InvokeMessage ObjectName="Video" MethodName="SetVolume" Parameters="0.5" />
> </IvokeMessages>
> ```

## XML 消息响应格式

```html
<InvokeResult StatusCode="" ExceptionMessage="" ObjectMethod="" ReturnType="" ReturnValue="">
	<Return Type="System.Int32">6</Return>
</InvokeResult>
```

| <div style="width:110pt;">**节点/@属性名称**</div> | <div style="width:400pt;">**说明**  </div>                                                                                            | <div style="width:40pt;">**值类型**</div> | <div style="width:50pt;">**必要** |
| :------------------------------------------- | :---------------------------------------------------------------------------------------------------------------------------------- | :------------------------------------- | :------------------------------ |
| **InvokeResults**                            | 调用远程方法或函数的返回消息集合，多个按顺序的响应消息集合                                                                                                       |                                        | 否，保留                            |
| **InvokeResult**                             | 调用远程方法或函数的返回消息对象，单个响应消息                                                                                                             |                                        | 是                               |
| @StatusCode                                  | 方法或函数执行的状态码，执行失败小于0，执行成功大于等于0；保留状态码：-2, -1, 0, 1                                                                                    | Int32                                  | 是                               |
| @ObjectMethod                                | 远程对象或实例的方法或函数的完整名称，格式：{ObjectName}.{MethodName}；示例：ObjectMethod="Window\.Close"                                                     | String                                 | 是                               |
| @ExceptionMessage                            | 方法或函数执行的异常信息，状态码为小于 0 的解释说明                                                                                                         | String                                 | 否                               |
| @ReturnType                                  | 节点 Return 的简单形式，**优先级低于 Return 节点**                                                                                                 | String                                 | 否                               |
| @ReturnValue                                 | 节点 Return 的简单形式，**优先级低于 Return 节点**                                                                                                 | String                                 | 否                               |
| **Return**                                   | 方法或函数的返回值，**优先级高于@ReturnType,@ReturnValue**                                                                                         |                                        | 否                               |
| @Type                                        | 返回的值类型，如果为System.Void类型，则值为 null；<br /> 参考：[TypeCode 枚举](https://learn.microsoft.com/zh-cn/dotnet/api/system.typecode?view=net-7.0) | String                                 | 否                               |
| @扩展属性或节点                                     | 可在 InvokeResult 根节点上扩展属性，或根节点之下扩展子节点(非Return节点)                                                                                     |                                        |                                 |

| **执行状 @StatusCode** | **状态码**  | **函数执行状态**                                          | **是否有返回值** |
| :------------------ | :------- | :-------------------------------------------------- | :--------- |
| Unknow              | -2       | 未知状态，函数可能执行成功，也有可能执行失败，可能是在传输过程中出现不可预测的异常，或是消息读写超时等 |            |
| Failed              | -1       | 确认执行失败                                              | 无          |
| Success             | 0        | 确认执行成功，函数无返回值为 System.Void 类型                       | 无          |
| SuccessAndReturn    | 1        | 确认执行成功，函数有返回值(为非 System.Void 类型)                    | 有          |
|                     | 其它自定义状态码 | 执行失败小于0，执行成功大于等于0                                   |            |

> #### 响应消息示例
>
> ```html
> <InvokeResult StatusCode="0" ObjectMethod="Window.Show" /> 
> <InvokeResult StatusCode="-1" ObjectMethod="Window.Close" ExceptionMessage="excption message content" />
> <InvokeResult StatusCode="1" ObjectMethod="Demo.OpenPage" ReturnType="System.Boolean" ReturnValue="True" />
> <InvokeResult StatusCode="1" ObjectMethod="Video.GetCurrentPosition">
> 	<Return Type="System.Float">5.6</Return>
> </InvokeResult>
>
> <!--同时响应返回多个执行消息状态-->
> <InvokeResults>
> 	<InvokeResult StatusCode="1" ObjectMethod="Video.Open" ReturnType="System.Boolean" ReturnValue="True"/>
> 	<InvokeResult StatusCode="0" ObjectMethod="Video.Play" />
> 	<InvokeResult StatusCode="0" ObjectMethod="Video.SetVolume"/>
> </InvokeResults>
> ```

## JSON 消息控制格式

```json
{
	"InvokeMessage":
	{
		"ObjectName":"",
		"MethodName":"",
		"Comment":"",
		//"Parameters":"", //考虑支持两种值类型？
		"Parameters":[
			{
				"Type":"",
				"Value":""
			},
			{
				"Type":"",
				"Value":""
			}
		]		
	}
}
// 同时执行多个控制消息
{
	"InvokeMessages":[
		{
			"ObjectName":"",
			"MethodName":"",
			"Parameters":"",
			"Comment":""
		},
		{
			"ObjectName":"",
			"MethodName":"",
			"Comment":"",
			"Parameters":[
				{
					"Type":"",
					"Value":""
				},
				{
					"Type":"",
					"Value":""
				}
			]
		}
	]
}
```

## JSON 消息响应格式

```json
{
	"InvokeResult":
	{
		"StatusCode":"",
		"ExceptionMessage":"",
		"ObjectMethod":"",
		"ReturnType":"",
		"ReturnValue":""
	}
}
// 多个响应消息
{
	"InvokeResults":[
		{
			"StatusCode":"",
			"ExceptionMessage":"",
			"ObjectMethod":"",
			"ReturnType":"",
			"ReturnValue":""
		},
		{
			"StatusCode":"",
			"ExceptionMessage":"",
			"ObjectMethod":"",
			"ReturnType":"",
			"ReturnValue":""
		}
	]
}
```

## 属性 @Parameters 的约定

*   多个参数值以英文 ',' 符号间隔区分，不用明确值类型
*   支持集合类型参数，在 '\[]' 符号内定义数据，元素类型为基本的值类型
*   支持识别十六进制字符内容，以 '0x' 开头的字符
*   支持字符串识别，以单引号或双引号包裹内的字符
*   示例：Parameters="12,play,1024,\[0x01,0xA0,0xAA],'this is string content'"
    ```java
    // 正则表达式对 @Parameters 分析示例
            /// <summary> 正则匹配 '~' | "~" </summary>
            internal static readonly String pattern_string = @"\'([^\']+)\'|" + "\"([^\"]+)\"";
            /// <summary> 正则匹配 '['~']' </summary>
            internal static readonly String pattern_array = @"\[([^\[\]]+)\]";
    #pragma warning disable CS0414
            /// <summary> 正则匹配 '('~')' </summary>
            internal static readonly String pattern_parent = @"\(([^\(\)]+)\)";
    #pragma warning restore CS0414
            /// <summary> 正则匹配 ',' 分割, 或结尾部份 </summary>
            internal static readonly String pattern_split = @"([^\,\'\[\]]+),|([^\,\'\[\]]+)$";
            internal static readonly String pattern_arguments = $@"{pattern_string}|{pattern_array}|{pattern_split}";
            /// <summary>
            /// 字符串参数正则表达式
            /// </summary>
            public static readonly Regex RegexStringParameters = new Regex(pattern_arguments, RegexOptions.Compiled | RegexOptions.Singleline);

            /// <summary>
            /// 将字符串参数集，分割转换为字符串数组
            /// <code>示例：
            /// 输入字符串："0x01,True,32,False"，输出数组：["0x01","True","32","False"]
            /// 输入字符串："0x01,3,[True,True,False]"，输出数组：["0x01","3",["True","True","False"]]
            /// 输入字符串："0x01,[0,3,4,7],[True,True,False,True]"，输出数组：["0x01",["0","3","4","7"],["True","True","False","True"]]
            /// 输入字符串："'hello,world',0x01,3,'ni?,hao,[aa,bb]', [True,True,False],['aaa,bb,c','ni,hao'],15,\"aa,aaa\",15"
            /// 输出数组：["hello,world","0x01","3","ni?,hao,[aa,bb]",["True","True","False","True"],["aaa,bb,c","ni,hao"],"15","aa,aaa","15"]
            /// </code>
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
                MatchCollection matchs = RegexStringParameters.Matches(parameters);
    #endif

                List<object> args = new List<object>();
                foreach (Match match in matchs)
                {
                    if (!match.Success) continue;
    #if true
                    string trimValue = match.Value.Trim();
                    char first = trimValue[0];
                    char last = trimValue[trimValue.Length - 1];

                    if (first == '\'' && last == '\'' || first == '\"' && last == '\"')
                    {
                        args.Add(trimValue.Substring(1, trimValue.Length - 2));
                    }
                    else if (first == '[' && last == ']')
                    {
                        args.Add(SplitToObjectArray(trimValue.Substring(1, trimValue.Length - 2)));
                    }
                    else if (last == ',')
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
    ```

## 消息加密等级参考

| 等级      | 说明          | 参考算法               |
| :------ | :---------- | :----------------- |
| 0级 \[x] | 明码传输，不做任何处理 |                    |
| 1级      | 隐藏，二次编码     | Base64, 其它自定义二进制序列 |
| 2级      | 对称加密        | AES, DES 等         |
| 3级      | 非对称加密       | RSA, DSA 等         |

