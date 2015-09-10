using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ColysSharp
{
    public class Utility
    {
        /// <summary>
        /// 转换为int值，如果非数值类型，则为0
        /// </summary>
        /// <param name="numberObj"></param>
        /// <returns></returns>
        public static int ToInt32(object numberObj) {
            int value;
            int.TryParse(numberObj.ToString(), out value);
            return value;
        }
        /// <summary>
        /// int值，如果非数值类型，则为nullValue
        /// </summary>
        /// <param name="numberObj"></param>
        /// <param name="nullValue"></param>
        /// <returns></returns>
        public static int ToInt32(object numberObj, int nullValue)
        {
            int value;
            if (!int.TryParse(numberObj.ToString(), out value)) value = nullValue;
            return value;
        }
        /// <summary>
        /// 记录异常Log4Net
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="appendMsg"></param>
        public static void LogException(Exception ex, string appendMsg = null)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(appendMsg == null ? "system" : appendMsg);
            log.Error(ex.Message, ex);
        }
        /// <summary>
        /// 记录异常Log4Net,并且给JsonMessage写入
        /// </summary>
        /// <param name="jm"></param>
        /// <param name="ex"></param>
        public static void LogException(ColysSharp.Modals.JsonMessage jm, Exception ex)
        {
            if (ex is ClientException) jm.Message = ex.Message;
            else jm.Message = "服务器出错！";
            LogException(ex, null);
        }


        /// <summary>
        /// 记录info信息到日志文件
        /// </summary>
        /// <param name="jm"></param>
        /// <param name="ex"></param>
        public static void LogMessage(string message,string moduleName="系统")
        {
            log4net.ILog log = log4net.LogManager.GetLogger(moduleName);
            log.Info(message);
        }


        /// <summary>
        /// Base64解密
        /// </summary>
        /// <param name="result">待解密的密文</param>
        /// <returns>解密后的字符串</returns>
        public static string DecodeBase64(string result)
        {

            byte[] bytes = Convert.FromBase64String(result);
            return Encoding.UTF8.GetString(bytes);
        }

    }
}
