using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace InsCache
{
    public static class InsCacheServiceExtensions
    {
        public static IServiceCollection AddInsCache(this IServiceCollection service)
        {
            service.AddSingleton<HashRoute>();
            service.AddSingleton<InsDictManager>();
            service.AddSingleton<InsCache>();
            return service;
        }
    }
}
