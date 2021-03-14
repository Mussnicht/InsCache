using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InsCache
{

    public class InsCache
    {
        private readonly InsDictManager insDictManager;
        public InsCache(InsDictManager _insDictManager)
        {
            insDictManager = _insDictManager;
        }

        public InsCacheExecutor<T> Build<T>() where T:class
        {
            return new InsCacheExecutor<T>(insDictManager);
        }
        public async Task<T> GetValue<T>(string key, Func<Task<T>> ormFunc = null, Func<Task<T>> redisFunc = null, Action<string, T> syncRedis = null, int? expirationTime = null,bool fromRedisOrDb = false) where T : class
        {
            if (string.IsNullOrEmpty(key)) throw new Exception("key不可为空");
            if (ormFunc == null && redisFunc == null) throw new Exception("至少选择一样获取key的方法以供调用：WithOrm,WhithRedis");
            return await insDictManager.GetValue(key, ormFunc, redisFunc, syncRedis, expirationTime,fromRedisOrDb);
        }
    }
}
