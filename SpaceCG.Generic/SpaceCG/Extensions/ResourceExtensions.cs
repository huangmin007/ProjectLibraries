using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// 资源管理扩展
    /// </summary>
    public static class ResourceExtensions
    {
        static Dictionary<String, Assembly> Dlls = new Dictionary<string, Assembly>();
        static Dictionary<String, Object> Assemblies = new Dictionary<string, Object>();

        /// <summary>
        /// 注册嵌入的 DLL 文件
        /// </summary>
        public static void RegisterDll()
        {
            Assembly module = new StackTrace(0).GetFrame(1).GetMethod().Module.Assembly;
            if (Assemblies.ContainsKey(module.FullName)) return;
            Assemblies.Add(module.FullName, null);

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            String[] resourceNames = module.GetManifestResourceNames();
            foreach (var name in resourceNames)
            {
                if (!name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) continue;
                
                try
                {
                    Stream stream = module.GetManifestResourceStream(name);
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, (int)stream.Length);
                    Assembly assembly = Assembly.Load(bytes);

                    if (Dlls.ContainsKey(assembly.FullName)) continue;
                    Dlls[assembly.FullName] = assembly;
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex);
                }
            }
        }
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            String fullName = new AssemblyName(args.Name).FullName;
            if (Dlls.TryGetValue(fullName, out Assembly assembly) && assembly != null)
            {
                Dlls[fullName] = null;
                return assembly;
            }
            else
            {
                throw new DllNotFoundException(fullName);
            }
        }
    }
}
