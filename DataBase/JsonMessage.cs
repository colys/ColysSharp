using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ColysSharp.Modals
{
    public class JsonMessage
    {
        public string Message { get; set; }

        public object Result { get; set; }

//        public void LogException(Exception ex)
//        {
//            Message = ex.Message;
//            Result = null;
//        }
//        public void LogException(Exception ex, string appendMsg)
//        {
//            if (ex == null) Message = appendMsg;
//            else
//            {
//                Message = ex.Message + " " + appendMsg;
//#if  DEBUG
//                Message += " \n" + ex.StackTrace;
//#endif
//            }
//            Result = null;
//        }
    }

    public class QJsonMessage : JsonMessage
    {
        public int Total { get; set; }
    }

    public class JsonMessage<T>
    {
        public string Message { get; set; }

        public T Result { get; set; }

        public void LogException(Exception ex)
        {
            Message = ex.Message;
#if  DEBUG
            Message += ex.StackTrace;
#endif
        }
    }
}
