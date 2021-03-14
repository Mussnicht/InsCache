using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace InsCache
{
    /// <summary>
    /// 路由
    /// </summary>
    public class HashRoute
    {
        private readonly SortedDictionary<ulong, string> _circle = new SortedDictionary<ulong, string>();
     
        /// <summary>
        /// 添加节点
        /// </summary>
        /// <param name="node">节点</param>
        /// <param name="repeat">分片：分片值越大，key存储的越均匀</param>
        public void AddNode(string node, int repeat)
        {
            for (int i = 0; i < repeat; i++)
            {
                string id = node.GetHashCode().ToString() + "_" + i;
                ulong hashCode = Md5Hash(id);
                _circle.Add(hashCode, node);
            }
        }

        private ulong Md5Hash(string id)
        {
            using (var hash = MD5.Create())
            {
                byte[] data = hash.ComputeHash(Encoding.UTF8.GetBytes(id));
                var a = BitConverter.ToUInt64(data, 0);
                var b = BitConverter.ToUInt64(data, 8);
                ulong hashCode = a ^ b;
                return hashCode;
            }
        }

        /// <summary>
        /// 获取指定key对应的节点
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetTargetNode(string key)
        {
            ulong hash = Md5Hash(key);
            ulong firstNode = ModifiedBinarySearch(_circle.Keys.ToArray(), hash);
            return _circle[firstNode];
        }

        private ulong ModifiedBinarySearch(ulong[] sortedArray, ulong val)
        {
            int min = 0;
            int max = sortedArray.Length - 1;

            if (val < sortedArray[min] || val > sortedArray[max])
                return sortedArray[0];

            while (max - min > 1)
            {
                int mid = (max + min) / 2;
                if (sortedArray[mid] >= val)
                {
                    max = mid;
                }
                else
                {
                    min = mid;
                }
            }

            return sortedArray[max];
        }

    }
}
