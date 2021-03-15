using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InsCache;
using System.Diagnostics;

namespace TestApi.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly InsCache.InsCache insCache;

        public TestController(InsCache.InsCache _insCache)
        {
            insCache = _insCache;
        }
        [HttpGet]
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

        [HttpGet]
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
    }

    public class user
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
