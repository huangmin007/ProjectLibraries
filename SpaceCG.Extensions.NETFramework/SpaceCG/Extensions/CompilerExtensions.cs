using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;

namespace SpaceCG.Extensions
{
    public static class CompilerExtensions
    {
        private static string GetCodes(string expression)
        {
            string code = @"
                using System;

                class TempClass
                {
                    public static object Evaluate()
                    {
                        return {0};
                    }
                }
            ";

            return code.Replace("{0}", expression);
        }

        /// <summary>
        /// <para>see: https://learn.microsoft.com/zh-cn/dotnet/api/microsoft.csharp.csharpcodeprovider?view=netframework-4.7 </para>
        /// <para>see: https://www.cnblogs.com/yidanda/archive/2009/07/20/1526978.html </para>
        /// </summary>
        /// <returns></returns>
        public static object Compiler()
        {
            String code = GetCodes("15+16");
            object result = null;
            using (CSharpCodeProvider csharpComplier = new CSharpCodeProvider())
            {
                CompilerParameters options = new CompilerParameters();
                options.GenerateExecutable = false;
                options.GenerateInMemory = true;

                CompilerResults results = csharpComplier.CompileAssemblyFromSource(options, code);

                if (results.Errors.Count > 0)
                {
                    return null;
                }

                Assembly assembly = results.CompiledAssembly;
                object instance = assembly.CreateInstance("TempClass");
                MethodInfo method = instance.GetType().GetMethod("Evaluate");
                result = method.Invoke(instance, null);

                Console.WriteLine($"Compile::{result}");
            }

            Console.WriteLine($"Result::{result}");
            return result;
        }

        /// <summary>
        /// Roslyn 编译器
        /// <para>see: https://www.cnblogs.com/dongweian/p/15773934.html </para>
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static object CompilerCSharpScript(string expression)
        {
            //string expression = "(1+2)*3/4";
            //var res = CSharpScript.EvaluateAsync<float>(expression);

            return null;
        }
    }
}
