using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace InsCache
{
    /// <summary>
    /// 存储索引，线程安全
    /// </summary>
    public class ConcurrentList:List<(string,long)>
    {
        private object lockObj = new object();
        public ConcurrentList()
        {
        }
        private bool LockExecute(Action act,int wait)
        {
            if (Monitor.TryEnter(lockObj, wait))
            {
                try
                {
                    act();
                    Monitor.Exit(lockObj);
                    return true;
                }
                catch (Exception)
                {
                    Monitor.Exit(lockObj);
                    return false;
                }
            }
            else
            {
                throw new Exception("操作超时");
            }
        }
        public void Lock()
        {
            if (!Monitor.TryEnter(lockObj, 2000))
            {
                throw new Exception("操作超时");
            }
        }
        public void Exist()
        {
            Monitor.Exit(lockObj);
        }
        public bool TryRemoveAt(int index,int wait = 2000)
        {
            return LockExecute(() =>
            {
                base.RemoveAt(index);
            }, wait);
        }
        public bool TryAdd(string key,long timeSpan,int wait = 2000)
        {
            return LockExecute(() =>
            {
                base.Add((key, timeSpan));
            }, wait);
        }
        public bool TryInsert(int index,string key,long timeSpan,int wait = 2000)
        {
            return LockExecute(() =>
            {
                base.Insert(index,(key, timeSpan));
            }, wait);
        }

    }
}
