using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// FileExtensions
    /// </summary>
    public static class FileExtensions
    {

        /// <summary>
        /// 保留目录中的文件数量
        /// <para>跟据文件创建日期排序，保留 count 个最新文件，超出 count 数量的文件删除</para>
        /// <para>注意：该函数是比较文件的创建日期</para>
        /// </summary>
        /// <param name="count">要保留的数量</param>
        /// <param name="path">文件目录，当前目录 "/" 表示，不可为空</param>
        /// <param name="searchPattern">只在目录中(不包括子目录)，查找匹配的文件；例如："*.jpg" 或 "temp_*.png"</param>
        public static void ReserveFileCount(int count, string path, string searchPattern = null)
        {
            if (count < 0 || String.IsNullOrWhiteSpace(path)) throw new ArgumentException("参数错误");

            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] files = searchPattern == null ? dir.GetFiles() : dir.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);

            if (files.Length <= count) return;

            //按文件的创建时间，升序排序(最新创建的排在最前面)
            Array.Sort(files, (f1, f2) =>
            {
                return f2.CreationTime.CompareTo(f1.CreationTime);
            });

            for (int i = count; i < files.Length; i++)
            {
                files[i].Delete();
            }
        }

        /// <summary>
        /// 保留目录中的文件天数
        /// <para>跟据文件上次修时间起计算，保留 days 天的文件，超出 days 天的文件删除</para>
        /// <para>注意：该函数是比较文件的上次修改日期</para>
        /// </summary>
        /// <param name="days">保留天数</param>
        /// <param name="path">文件夹目录</param>
        /// <param name="searchPattern">文件匹配类型, 只在目录中(不包括子目录)，查找匹配的文件；例如："*.jpg" 或 "temp_*.png"</param>
        public static void ReserveFileDays(int days, string path, string searchPattern = null)
        {
            if (days < 0 || String.IsNullOrWhiteSpace(path)) return;

            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] files = searchPattern == null ? dir.GetFiles() : dir.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
            if (files.Length == 0) return;

            IEnumerable<FileInfo> removes =
                  from file in files
                  where file.LastWriteTime < DateTime.Today.AddDays(-days)
                  select file;

            foreach (var file in removes)
            {
                file.Delete();
            }
        }

    }
}
