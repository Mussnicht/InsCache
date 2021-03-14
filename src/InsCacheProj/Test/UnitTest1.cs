using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            InsCache.InsCache insCache = new InsCache.InsCache(new InsCache.InsDictManager(new InsCache.HashRoute(), null));
            await insCache.Build<string>()
                    .SetExpirationTime(30)
                    .FromRedisOrDb()
                    .WithRedis(() => { return Task.FromResult("value"); })
                    .WithOrm(() => { return Task.FromResult("Value"); })
                    .GetValue("key");

        }
    }
}
