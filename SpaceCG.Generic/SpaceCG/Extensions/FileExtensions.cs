using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// FileExtensions
    /// </summary>
    public static partial class FileExtensions
    {
        /// <summary>
        /// 文件的异步复制，参考 <see cref="File.Copy(string, string, bool)"/>
        /// </summary>
        /// <param name="source">源文件路径</param>
        /// <param name="destination">目标文件路径</param>
        /// <param name="overwrite">是否覆盖</param>
        /// <param name="progress">进度报告</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public static async Task CopyAsync(string source, string destination, bool overwrite = true, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(source)) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(destination)) throw new ArgumentNullException(nameof(destination));

            if (!File.Exists(source)) throw new FileNotFoundException(source);

            string destDirectory = Path.GetDirectoryName(destination);
            if (!Directory.Exists(destDirectory)) Directory.CreateDirectory(destDirectory);

            const int CopyBufferSize = 1024 * 256;
            using (FileStream sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, CopyBufferSize))
            {
                using (FileStream destinationStream = new FileStream(destination, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None, CopyBufferSize))
                {
                    if (progress == null)
                    {
                        await sourceStream.CopyToAsync(destinationStream, CopyBufferSize, cancellationToken);
                    }
                    else
                    {
                        _ = sourceStream.CopyToAsync(destinationStream, CopyBufferSize, cancellationToken);
                        await destinationStream.LengthProgressReport(sourceStream.Length, progress, cancellationToken);
                    }
                }
            }

            File.SetLastWriteTimeUtc(destination, File.GetLastWriteTimeUtc(source));
            //File.SetCreationTimeUtc(destination, File.GetCreationTimeUtc(source));
            //File.SetLastAccessTimeUtc(destination, File.GetLastAccessTimeUtc(source));
        }

        /// <inheritdoc cref="CopyAsync(string, string, bool, IProgress{float}, CancellationToken)"/>
        public static Task CopyAsync(FileInfo source, FileInfo destination, bool overwrite = true, IProgress<float> progress = null, CancellationToken cancellationToken = default)
            => CopyAsync(source.FullName, destination.FullName, overwrite, progress, cancellationToken);


        /// <summary>}
        /// 文件的异步移动，参考 <see cref="File.Move(string, string)"/>
        /// </summary>
        /// <param name="source">源文件路径</param>
        /// <param name="destination">目标文件路径</param>
        /// <param name="overwrite">是否覆盖</param>
        /// <param name="progress">进度报告</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public static async Task MoveAsync(string source, string destination, bool overwrite = true, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            await CopyAsync(source, destination, overwrite, progress, cancellationToken);
            File.Delete(source);
        }

        /// <inheritdoc cref="MoveAsync(string, string, bool, IProgress{float}, CancellationToken)"/>
        public static Task MoveAsync(FileInfo source, FileInfo destination, bool overwrite = true, IProgress<float> progress = null, CancellationToken cancellationToken = default)
            => MoveAsync(source.FullName, destination.FullName, overwrite, progress, cancellationToken);


        /// <summary>
        /// 保留目录中的文件数量
        /// <para>跟据文件创建日期排序，保留 count 个最新文件，超出 count 数量的文件删除</para>
        /// <para>注意：该函数是比较文件的创建日期</para>
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="count">要保留的数量</param>
        /// <param name="searchPattern">只在目录中(不包括子目录)，查找匹配的文件；例如："*.jpg" 或 "temp_*.png"</param>
        public static void ReserveFileCount(string dirPath, int count, string searchPattern = null)
        {
            if (count < 0 || string.IsNullOrWhiteSpace(dirPath) || !Directory.Exists(dirPath)) throw new ArgumentException("参数错误");

            DirectoryInfo dir = new DirectoryInfo(dirPath);
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
        /// <param name="dirPath">文件夹目录</param>
        /// <param name="days">保留天数</param>
        /// <param name="searchPattern">文件匹配类型, 只在目录中(不包括子目录)，查找匹配的文件；例如："*.jpg" 或 "temp_*.png"</param>
        public static void ReserveFileDays(string dirPath, int days,  string searchPattern = null)
        {
            if (days < 0 || string.IsNullOrWhiteSpace(dirPath) || !Directory.Exists(dirPath)) throw new ArgumentException("参数错误");

            DirectoryInfo dir = new DirectoryInfo(dirPath);
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

        /// <summary>
        /// 保留目录中的文件数量
        /// <para>跟据文件创建日期排序，保留 count 个最新文件，超出 count 数量的文件删除</para>
        /// <para>注意：该函数是比较文件的创建日期</para>
        /// </summary>
        /// <param name="dirInfo"></param>
        /// <param name="count"></param>
        /// <param name="searchPattern"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void ReserveFileCount(this DirectoryInfo dirInfo, int count, string searchPattern = null)
        {
            if(dirInfo == null || !dirInfo.Exists) throw new ArgumentNullException(nameof(dirInfo));
            if (count < 0) throw new ArgumentException("参数错误");

            FileInfo[] files = searchPattern == null ? dirInfo.GetFiles() : dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);

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
        /// <param name="dirInfo"></param>
        /// <param name="days"></param>
        /// <param name="searchPattern"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void ReserveFileDays(this DirectoryInfo dirInfo, int days, string searchPattern = null)
        {
            if (dirInfo == null || !dirInfo.Exists) throw new ArgumentNullException(nameof(dirInfo));
            if (days < 0) throw new ArgumentException("参数错误");

            FileInfo[] files = searchPattern == null ? dirInfo.GetFiles() : dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
            if (files.Length == 0) return;

            var removes = from file in files
                          where file.LastWriteTime < DateTime.Today.AddDays(-days)
                          select file;

            foreach (var file in removes)
            {
                file.Delete();
            }
        }

        /// <summary>
        /// 判断文件系统信息是否存在于集合中
        /// </summary>
        /// <param name="fileSystemInfos"></param>
        /// <param name="fileName"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public static bool Contains(this IEnumerable<FileSystemInfo> fileSystemInfos, string fileName, IEqualityComparer<FileSystemInfo> comparer)
        {
            if (File.Exists(fileName))
            {
                return fileSystemInfos.Contains(new FileInfo(fileName), comparer);
            }
            else if (Directory.Exists(fileName))
            {
                return fileSystemInfos.Contains(new DirectoryInfo(fileName), comparer);
            }

            return false;
        }

        /// <summary>
        /// 对比两个文件是否相同是同一个文件
        /// </summary>
        public class FileSystemInfoComparer : IEqualityComparer<FileSystemInfo>
        {
            /// <summary>
            /// 文件系统信息全名比较器
            /// </summary>
            public static readonly FileSystemInfoComparer FullNameComparison = new FileSystemInfoComparer();

            /// <inheritdoc />
            public bool Equals(FileSystemInfo x, FileSystemInfo y)
            {
                if (x == null || y == null) return false;
                if (!x.Exists || !y.Exists) return false;
                if (x.Attributes != y.Attributes) return false;
                if (x.CreationTime != y.CreationTime) return false;

                string xFileFullName = x.FullName;
                if ((x.Attributes & FileAttributes.Directory) == FileAttributes.Directory && xFileFullName.EndsWith($"{Path.DirectorySeparatorChar}"))
                    xFileFullName = x.FullName.TrimEnd(Path.DirectorySeparatorChar);

                string yFileFullName = y.FullName;
                if ((y.Attributes & FileAttributes.Directory) == FileAttributes.Directory && yFileFullName.EndsWith($"{Path.DirectorySeparatorChar}"))
                    yFileFullName = y.FullName.TrimEnd(Path.DirectorySeparatorChar);

                return xFileFullName.Equals(yFileFullName, StringComparison.OrdinalIgnoreCase);
            }

            /// <inheritdoc />
            public int GetHashCode(FileSystemInfo fileInfo)
            {
                return fileInfo.Name.GetHashCode();
            }
        }
    }
}
