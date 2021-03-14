using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace InsCache
{
    /// <summary>
    /// 字典管理类，限制查询/返回，驱动字典存取等
    /// </summary>
    public class InsDictManager
    {
        private int timeOut = 5;//超时时间。
        private int clearInterval = 20;//清理过期间隔
        private int count = 10;//使用多少个字典存储
        private Dictionary<string, InsDict> Db;//key对应的字典
        private Timer clearExpirationTimer;//定时清理过期
        private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> lockTcs;
        private readonly HashRoute hashRoute;//路由

        public InsDictManager(HashRoute _hashRoute,IConfiguration _configuration)
        {
            #region 初始化和注入
            hashRoute = _hashRoute;
            Db = new Dictionary<string, InsDict>();
            lockTcs = new ConcurrentDictionary<string, TaskCompletionSource<object>>();
            #endregion

            #region 根据配置修改私有变量
            if (_configuration != null)
            {
                var selection = _configuration.GetSection("InsCache");
                if (selection["TimeOut"] != null) { timeOut = Convert.ToInt32(selection["TimeOut"]); };
                if (selection["DictCount"] != null) { count = Convert.ToInt32(selection["DictCount"]); };
                if (selection["OverdueClearInterval"] != null) { clearInterval = Convert.ToInt32(selection["OverdueClearInterval"]); };
            }
            #endregion

            #region 根据配置的count添加路由节点和对应db
            Enumerable.Range(0, count).ToList().ForEach(i =>
            {
                var id = "dict" + i.ToString();
                Db.Add(id, new InsDict());
                hashRoute.AddNode(id, 10);
            });
            #endregion

            #region 定时清理过期
            clearExpirationTimer = new Timer(clearInterval*1000);
            clearExpirationTimer.Elapsed += async (o, e) =>
            {
                await ClearExpirationDataSync();
            };
            clearExpirationTimer.Start();
            #endregion
        }
        /// <summary>
        /// 根据路由获取db
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private InsDict GetDb(string key)
        {
            return Db[hashRoute.GetTargetNode(key)];
        }
        /// <summary>
        /// 清除过期
        /// </summary>
        /// <returns></returns>
        private Task ClearExpirationDataSync()
        {
            Db.ToList().ForEach(async dict =>
            {
                await dict.Value.ClearExpirationSync();
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// 获取值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">key</param>
        /// <param name="func">数据库取值方法:存在则最后调用</param>
        /// <param name="redisFunc">redis取值方法:存在则优先调用</param>
        /// <param name="SyncRedis">同步redis方法:存在则取值后且值不为空才调用</param>
        /// <param name="expirationTime">过期时间</param>
        /// <param name="fromRedisOrDb">从Redis或数据库取值，默认false</param>
        /// <returns></returns>
        public async Task<T> GetValue<T>(string key, Func<Task<T>> func,Func<Task<T>> redisFunc,Action<string,T> SyncRedis,int? expirationTime,bool fromRedisOrDb) where T:class
        {
            var db = GetDb(key);
            InsValue res;
            var getRes = await db.GetValue(key, out res, fromRedisOrDb);
            if (!getRes)
            {
                var locker = lockTcs.TryAdd(key,new TaskCompletionSource<object>());
                if (locker)
                {
                    #region 超时处理
                    Timer overtime = new Timer(timeOut*1000);
                    overtime.AutoReset = false;//只执行一次。
                    overtime.Elapsed += (o, e) =>
                    {
                        TaskCompletionSource<object> _outer;
                        lockTcs.TryGetValue(key, out _outer);
                        _outer.SetException(new Exception("已超时"));
                        lockTcs.TryRemove(key, out _outer);//移除locker
                        throw new Exception("已超时");
                    };
                    overtime.Start();
                    #endregion

                    #region tcs通知,定时器移除
                    TaskCompletionSource<object> outer;
                    void SetAndRemoveTcs(T v = null,Exception exception = null)
                    {
                        overtime.Stop();
                        overtime = null;
                        lockTcs.TryGetValue(key, out outer);
                        if (exception == null)
                        {
                            outer?.SetResult(v);
                        }
                        else
                        {
                            outer?.SetException(exception);
                        }
                        lockTcs.TryRemove(key, out outer);
                    }
                    #endregion

                    #region 获取数据:先从redis获取，如果失败再从数据库获取。
                    T result = null;
                    try
                    {
                        T redisRes = null;
                        if (redisFunc != null)
                        {
                            result = await redisFunc();
                            redisRes = result;
                        }
                        if (result == null && func != null)
                        {
                            result = await func();
                        }
                        if(redisRes==null && result!=null && SyncRedis != null)
                        {
                            SyncRedis(key,result);
                        }
                    }catch(Exception ex)
                    {
                        SetAndRemoveTcs(exception:ex);
                        throw ex;
                    }

                    #endregion

                    #region 存储并通知其他请求。
                    await SetValue(key, result, expirationTime);//存储
                    SetAndRemoveTcs(result);
                    return result;
                    #endregion
                }
                else
                {
                    var awaitRes = await lockTcs[key].Task;
                    return awaitRes==null?null:(T)awaitRes; 
                }
            }
            return res.Value == null ? null : (T)res.Value;
        }
        private async Task SetValue(string key, object value,int? expirationTime)
        {
            var db = GetDb(key);
            if (expirationTime == null)
            {
                await db.SetValue(key, value);
            }
            else
            {
                await db.SetValue(key, value, (int)expirationTime);
            }
            
        }
    }
}
