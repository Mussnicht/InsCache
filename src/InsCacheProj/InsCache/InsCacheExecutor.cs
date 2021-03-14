using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InsCache
{
    /// <summary>
    /// 链式调用
    /// </summary>
    public class InsCacheExecutor<T> where T:class
    {
        private readonly InsDictManager insDictManager;
        private int? _expirationTime = null;
        private Func<Task<T>> _ormFunc = null;
        private Func<Task<T>> _redisFunc = null;
        private Action<string,T> _syncRedis = null;
        private bool _fromRedisOrDb = false;
        public InsCacheExecutor(InsDictManager _insDictManager)
        {
            insDictManager = _insDictManager;
        }
        /// <summary>
        /// 设置过期时间
        /// </summary>
        /// <param name="time">单位:秒(s)</param>
        /// <returns></returns>
        public InsCacheExecutor<T> SetExpirationTime(int? time)
        {
            _expirationTime = time;
            return this;
        }
        /// <summary>
        /// 数据库查询操作
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public InsCacheExecutor<T> WithOrm(Func<Task<T>> func) 
        {
            _ormFunc = func;
            return this;
        }
        /// <summary>
        /// 是否直接从Redis/数据库查询：并发请求仍只有一个查询Redis或数据库。
        /// </summary>
        /// <returns></returns>
        public InsCacheExecutor<T> FromRedisOrDb()
        {
            _fromRedisOrDb = true;
            return this;
        }
        /// <summary>
        /// Redis查询操作
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public InsCacheExecutor<T> WithRedis(Func<Task<T>> func) 
        {
            _redisFunc = func;
            return this;
        }
        /// <summary>
        /// 同步Redis操作
        /// </summary>
        /// <param name="act"></param>
        /// <returns></returns>
        public InsCacheExecutor<T> SyncRedis(Action<string, T> act) 
        {
            _syncRedis = act;
            return this;
        }
        /// <summary>
        /// 获取缓存值
        /// </summary>
        /// <param name="key">key</param>
        /// <returns></returns>
        public async Task<T> GetValue(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new Exception("key不可为空");
            if (_ormFunc == null && _redisFunc == null) throw new Exception("至少选择一样获取key的方法以供调用：WithOrm,WhithRedis");
            return await insDictManager.GetValue(key, _ormFunc, _redisFunc, _syncRedis, _expirationTime,_fromRedisOrDb);
        }
        
    }
}
