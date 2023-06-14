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
                    public static object Compiler()
                    {
                        return {0};
                    }
                }
            ";

            return code.Replace("{0}", expression);
        }

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
                MethodInfo method = instance.GetType().GetMethod("Compiler");
                result = method.Invoke(instance, null);

                Console.WriteLine($"Compile::{result}");
            }

            Console.WriteLine($"Result::{result}");
            return result;
        }

    }
}
