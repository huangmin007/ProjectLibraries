using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// Extensions
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// 从 <see cref="ConcurrentQueue&lt;T&gt;"/> 中移除所有对象。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queuq"></param>
        public static void Clear<T>(this ConcurrentQueue<T> queuq)
        {
            while (queuq.TryDequeue(out T result))
            {
                ;
            }
        }
    }
}
