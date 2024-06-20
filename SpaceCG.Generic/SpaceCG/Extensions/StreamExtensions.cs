using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Text;
using SpaceCG.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// Stream 扩展方法
    /// </summary>
    public static class StreamExtensions
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(StreamExtensions));

        /// <summary>
        /// HttpClient 下载文件，可以设置进度报告
        /// </summary>
        /// <param name="client"></param>
        /// <param name="requestUri">Uri 请求地址</param>
        /// <param name="fileSystemInfo">文件或是目录</param>
        /// <param name="progress">进度报告器，可为 null；在非空的情况下返回负值，表示下载失败或异常</param>
        /// <param name="cancellationToken">异步操作的取消令牌</param>
        /// <returns>返回 HttpStatusCode 和下载的文件名</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task<(HttpStatusCode StatusCode, string FileName)> DownloadFileAsync(this HttpClient client, Uri requestUri, FileSystemInfo fileSystemInfo, IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client), "参数不能为空");
            if (requestUri == null) throw new ArgumentNullException(nameof(requestUri), "参数不能为空");

            Logger.Info($"Ready To Download File : {requestUri}");
            if (fileSystemInfo == null) fileSystemInfo = new DirectoryInfo(Environment.CurrentDirectory);

            // 默认的复制缓冲区大小
            const int DefaultCopyBufferSize = 1024 * 256;   //default 1024 * 80

            var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead);
            Logger.Info($"Request Uri: {requestUri}  Respose Status Code: {response?.StatusCode}");
            if (!response.IsSuccessStatusCode) return (response.StatusCode, null);

            var contentLength = response.Content.Headers.ContentLength;
            var contentDisposition = response.Content.Headers.ContentDisposition;

            string fileName;
            if (fileSystemInfo is FileInfo file)
            {
                fileName = file.FullName;
            }
            else if (fileSystemInfo is DirectoryInfo dir)
            {
                fileName = Path.Combine(dir.FullName, contentDisposition?.FileName ?? Path.GetFileName(requestUri.LocalPath));
            }
            else
            {
                fileName = Path.Combine(Environment.CurrentDirectory, contentDisposition?.FileName ?? Path.GetFileName(requestUri.LocalPath));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            Logger.Info($"Request Uri: {requestUri}  Content Length: {contentLength}  File Path: {fileName}");

            try
            {
                using (var contentStream = await response.Content.ReadAsStreamAsync())
                {
                    using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, DefaultCopyBufferSize))
                    {
                        if (progress == null || contentLength == null)
                        {
                            await contentStream.CopyToAsync(fileStream, DefaultCopyBufferSize, cancellationToken);
                        }
                        else
                        {
                            _ = contentStream.CopyToAsync(fileStream, DefaultCopyBufferSize, cancellationToken);
                            await fileStream.LengthProgressReport(contentLength ?? 0, progress, cancellationToken);
                        }
                        Logger.Info($"Download File: {requestUri}  Content Length: {contentLength}  File Name: {fileName}  Download Successed");
                    }
                }
            }
            catch (Exception ex)
            {
                if (progress != null)
                    progress.Report(-1.0f);

                Logger.Error($"Download File: {requestUri}  Content Length: {contentLength}  File Name: {fileName}  Download Failed: {ex.Message}");
                Logger.Error($"Exception: {ex}");

                File.Delete(fileName);
                fileName = null;

                return (response.StatusCode, null);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Logger.Warn($"Download File: {requestUri}  Content Length: {contentLength}  File Name: {fileName}  Download Failed IsCancellationRequested.");

                File.Delete(fileName);
                fileName = null;
            }

            if (!string.IsNullOrWhiteSpace(fileName) && File.Exists(fileName))
            {
                //if (contentDisposition?.CreationDate != null)
                //    File.SetCreationTimeUtc(fileName, contentDisposition.CreationDate.Value.DateTime);
                if (contentDisposition?.ModificationDate != null)
                    File.SetLastWriteTimeUtc(fileName, contentDisposition.ModificationDate.Value.DateTime);
            }

            return (response.StatusCode, fileName);
        }

        /// <summary>
        /// 报告流的当前长度的异步操作进度
        /// <para>原理：通过实时读取流的长度，与目标长度比较，计算出进度，并通过进度报告器报告进度。</para>
        /// <para>如果异步操作过程中需要取消进度报告，可设 <paramref name="progress"/> 为 null 值，注意是取消进度报告，而不是取消异步操作。</para>
        /// </summary>
        /// <param name="stream">需要报告进度的流对象</param>
        /// <param name="length">流的目标长度（以字节为单位）</param>
        /// <param name="progress">报告进度的进度器，如果初使为 null，则不报告进度；在非空的情况下返回负值，表示下载失败或异常</param>
        /// <param name="cancellationToken">异步操作的取消令牌，与流的异步操作应使用相同的取消令牌</param>
        /// <returns></returns>
        public static async Task LengthProgressReport(this Stream stream, long length, IProgress<float> progress, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), "参数不能为空");

            if (length <= 0 || progress == null) return;

            try
            {
                var streamLength = stream.Length;
            }
            catch (NotSupportedException ex)
            {
                Logger.Warn($"当前流 {stream} 不支持 Length 属性的读取操作，无法报告进度。");
                return;
            }

            await Task.Run(async () =>
            {
                float lengthFloat = (float)length;

                try
                {
                    while (stream.Length < length)
                    {
                        await Task.Delay(20);   // 延迟 20 毫秒，避免过于频繁的进度报告

                        if (progress == null) break;
                        if (cancellationToken.IsCancellationRequested) break;

                        progress.Report(stream.Length / lengthFloat);
                    }
                }
                catch (Exception)
                {
                    progress.Report(-1.0f);
                    Logger.Warn($"异步操作取消，不报告进度。");
                }
            });
        }

        /// <summary>
        /// 报告流的当前位置的异步操作进度
        /// <para>原理：通过实时读取流的位置，与目标位置比较，计算出进度，并通过进度报告器报告进度。</para>
        /// <para>如果异步操作过程中需要取消进度报告，可设 <paramref name="progress"/> 为 null 值，注意是取消进度报告，而不是取消异步操作。</para>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="position"></param>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static async Task PositionProgressReport(this Stream stream, long position, IProgress<float> progress, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), "参数不能为空");

            if (position < 0 || progress == null) return;

            try
            {
                var streamPosition = stream.Position;
            }
            catch (NotSupportedException ex)
            {
                Logger.Warn($"当前流 {stream} 不支持 Position 属性的读取操作，无法报告进度。");
                return;
            }

            await Task.Run(async () =>
            {
                float positionFloat = (float)position;

                try
                {
                    while (stream.Position < position)
                    {
                        await Task.Delay(20);   // 延迟 20 毫秒，避免过于频繁的进度报告

                        if (progress == null) break;
                        if (cancellationToken.IsCancellationRequested) break;

                        progress.Report(stream.Position / positionFloat);
                    }
                }
                catch (Exception)
                {
                    progress.Report(-1.0f);
                    Logger.Warn($"异步操作取消，不报告进度。");
                }
            });
        }


        /// <summary>
        /// 从流中读取一行数据，并返回文本内容和长度
        /// <para>原理：读取流直到遇到换行符（CR LF）或流尾，并返回读取的数据和长度。</para>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static (string Content, int Length) ReadLine(this Stream stream, Encoding encoding)
        {
            const byte CR = 0x0D;   // 回车 13
            const byte LF = 0x0A;   // 换行 10

            bool lastIsCR = false;
            List<byte> buffer = new List<byte>(256);

            while (true)
            {
                int value = stream.ReadByte();
                if (value == -1) break;  // 读到流尾则结束

                buffer.Add((byte)value);

                if (lastIsCR && value == LF) break;  // 遇到 CR LF 则结束
                lastIsCR = (value == CR);
            }

            return (encoding.GetString(buffer.ToArray()), buffer.Count);
        }


    }
}
