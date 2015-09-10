using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ColysSharp
{
    /// <summary>
    /// 可显示在客户端的异常
    /// </summary>
    public class ClientException:Exception
    {
        public ClientException(string message) : base(message) { }

        public ClientException(string message,Exception inner) : base(message,inner) { }
    }
}
