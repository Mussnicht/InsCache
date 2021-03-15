# InsCache
高并发查询/多级缓存/进程缓存/Redis缓存
# 用途及原理
多级缓存方案，用于高并发查询优化。原理比较简单，举个例子：1000个key为a的请求和1000个key为b的请求同时发生，先从进程内查询a，b，如果没有获取到则查询Redis(如果配置了Redis)，如果再没有数据则查询Db，所以最多只会有2条请求(a和b)进入Redis/数据库，其余请求等待这两个请求完成后共享数据返回给客户端，极大减小了数据库的压力！
# 支持
net core,net5
# 用法
1. 下载InsCache类库项目并添加引用。
2. startup.cs中引入: service.AddInsCache
3. AppSettings.json配置
~~~json
"InsCache":{
  "TimeOut":"5",//超时时间，单位s，默认5
  "DictCount":"10",//用几个线程安全字典存储，默认10。内部使用一致性hash决定使用哪个字典。(感觉没什么用，但我不想全存一个字典)
  "OverdueClearInterval":"20"//多久清理一次过期，单位s，默认20。
}
~~~
4. contoller中使用
~~~c#
public class TestController:Controller
{
  private InsCache insCache; 
  public TestController(InsCache _insCache)
  {
    insCache = _insCache;//构造注入
  }
  [HttpGet]
  public async Task<object> Get(string key)
  {
    return await insCache.Build<User>()//泛型可指定为具体类，进程内存储为object
                    .SetExpirationTime(30)//设置过期时间为30s
                    .FromRedisOrDb()//使用redis或数据库查询，不使用进程内数据。（n个对于指定key的并发请求仍会只有1个进入）
                    .WithRedis(() => { return Task.FromResult("value"); })//用户自己写Redis查询操作
                    .WithOrm(() => { return Task.FromResult("Value"); })//用户自己写数据库查询操作
                    .SyncRedis((k,v)=>{})//用户自己写Redis同步操作
                    .GetValue("key");//指定key获取相应返回值
  }
}
~~~
# 部分实现说明 
1. 清理过期数据
* 对字典中每条数据维护了一个过期时间戳，在获取数据时会先判断一次时间戳，过期则返回null，所以即使数据过期，也不用立刻清除，那么请务必不要设置太小的清理时间间隔，很浪费性能；另外字典类中维护了线程安全的List类型索引，时间戳升序，每次只需遍历索引来清除部分过期数据。
2. 等待某个请求完成
* 使用线程安全字典加TaskCompletedSource实现，尽量使用已有功能。
3. FromRedisOrDb的作用
* 不想使用进程内的缓存，直接调用db或Redis，同样适用于并发，一人查多人用。
4. 可配置
* 可以选择不设置一些配置，例如不写WithRedis，只使用数据库；可以不设置过期时间永久缓存；不写SyncRedis同步；(但Redis/数据库必选其一或都选)
5. Orm和Redis操作
* 没有任何具体封装也没必要，用户可以选择使用自己喜欢的方式去写，最终只需返回Task\<T\>。
# Api测试
~~~c#
//对3个key分别请求1000次
public Task Get1000_3key(string key1,string key2,string key3)
        {
            Parallel.For(0, 1000, async i =>
            {
                await Get(key1);
            });
            Parallel.For(0, 1000, async i =>
            {
                await Get(key2);
            });
            Parallel.For(0, 1000, async i =>
            {
                await Get(key3);
            });
            return Task.CompletedTask;
        }
~~~
~~~c#
//Get方法
public async Task<user> Get(string key)
        {
            return await insCache.Build<user>()
                                 .SetExpirationTime(10)//缓存10s
                                 .WithRedis(async () =>
                                 {
                                     Debug.WriteLine($"请求Redis，{key}");
                                     await Task.Yield();
                                     return null;
                                 })
                                 .WithOrm(async () =>
                                 {
                                     Debug.WriteLine($"请求数据库，{key}");
                                     await Task.Yield();
                                     return new user { Id = "001", Name = key+"Name" };
                                 })
                                 .GetValue(key);
        }
~~~
运行api项目，swagger调用Get1000_3key，打开视图-->输出。发现仅分别打印三次“请求redis”、“请求数据库”。并且10s之内再次用相同key调用接口，不会再次请求redis/数据库。
# 备注
1. 并不一定需要copy着用，可参考方案自己实现，集思广益，这个帮助类本就是受群里大佬启发而写的。
2. InCache只做了这件事：用户指定一些列配置和操作，框架协调中间过程，最终达到减少数据库压力的目的。
