using System;
using System.Collections.Generic;
using System.Text;

namespace InsCache
{
    /// <summary>
    /// 字典值类型定义
    /// </summary>
    public class InsValue
    {
        public InsValue()
        {
            InsertTimeSpan = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds);
        }
        /// <summary>
        /// 存储时间
        /// </summary>
        public long InsertTimeSpan { get; set; }
        /// <summary>
        /// 过期时间：单位(s)
        /// </summary>
        public int ExpirationTime { get; set; }
        /// <summary>
        /// 开启过期模式：false则不会自动过期
        /// </summary>
        public bool OpenExpirationControl { get; set; }
        /// <summary>
        /// 字典值
        /// </summary>
        public object Value { get; set; }
    }
}
