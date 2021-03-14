using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace InsCache
{
    /// <summary>
    /// 字典封装
    /// </summary>
    public class InsDict
    {
        private ConcurrentList index;//线程安全，清除过期时加锁。
        private ConcurrentDictionary<string, InsValue> dict;
        public InsDict()
        {
            index = new ConcurrentList();
            dict = new ConcurrentDictionary<string, InsValue>();
            
        }
        private long GetTimeSpan()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds);
        }
        /// <summary>
        /// 清除过期
        /// </summary>
        /// <returns></returns>
        public async Task ClearExpirationSync()
        {
            await Task.Run(() =>
            {
                var nowSpan = GetTimeSpan();
                index.Lock();
                while (index.Count > 0 && index[0].Item2 < nowSpan)
                {
                    InsValue outer;
                    dict.TryRemove(index[0].Item1, out outer);
                    index.RemoveAt(0);
                }
                index.Exist();
            });
        }
        /// <summary>
        /// 获取key对应的值，out value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="res"></param>
        /// <returns>是否成功获取</returns>
        public Task<bool> GetValue(string key, out InsValue res,bool fromRedisOrDb)
        {
            var getRes = dict.TryGetValue(key, out res);
            if (getRes && !fromRedisOrDb && (!res.OpenExpirationControl || res.OpenExpirationControl && (res.InsertTimeSpan + res.ExpirationTime * 1000) > 0))
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);//如果过期或不存在则等待自动清除。
        }
        /// <summary>
        /// 设置value
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public Task SetValue(string key, object value)
        {
            dict[key] = new InsValue { Value = value, OpenExpirationControl = false };
            return Task.CompletedTask;
        }
        /// <summary>
        /// 设置value，可过期
        /// </summary>
        /// <param name="key">key</param>
        /// <param name="value">值</param>
        /// <param name="expirationTime">过期时间，秒</param>
        /// <returns></returns>
        public Task SetValue(string key, object value, int expirationTime)
        {
            var inserter = new InsValue { ExpirationTime = expirationTime, Value = value, OpenExpirationControl = true };
            dict[key] = inserter;
            //如果index中存在相同key，先删除。
            for (int idx = 0; idx < index.Count; idx++)
            {
                if (index[idx].Item1 == key)
                {
                    index.TryRemoveAt(idx);
                    break;
                }
            }
            //插入最新过期时间
            var expiration = inserter.InsertTimeSpan + inserter.ExpirationTime * 1000;
            int i = 0;
            while (i < index.Count && index[i].Item2 < expiration) { i++; };
            if (i == index.Count)
            {
                index.TryAdd(key, (long)expiration);
            }
            else
            {
                index.TryInsert(i, key, (long)expiration);
            }
            return Task.CompletedTask;
        }
    }
}
