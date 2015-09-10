using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using ColysSharp.DataBase;
using ColysSharp.Modals;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using ColysSharp.StaticHtml;

namespace ColysSharp.Mvc
{
    public class BaseController : Controller
    {
        /// <summary>
        /// 实体类全名，可重载，空则为全局配置
        /// </summary>
        protected virtual string EntityFormat { get { return null; } }
        public struct PaggerResult { public int sEcho; public int iTotalRecords; public int iTotalDisplayRecords; public string[][] aaData; }

        /// <summary>
        /// 执行列表查询操作，返回json字符串，如果请求中包含pageIndex，将会进行分页查询
        /// 树型查询需要传tree和parent参数
        /// </summary>
        /// <param name="permission">查询权限配置</param>
        /// <param name="queryField">查询的字符，以逗号隔开</param>
        /// <param name="entityName">查询的对象实体类名（不需要完整名,Name即可）</param>
        /// <param name="whereField">条件表达式json</param>
        /// <param name="orderBy">排序</param>
        /// <returns></returns>
        public virtual string DoQuery(string permission, string queryField, string entityName, string whereField, string orderBy)
        {
            QJsonMessage jr = new QJsonMessage();
            DBContext db = new DBContext(EntityFormat);
            try
            {
                whereField = System.Web.HttpUtility.UrlDecode(whereField);
                Dictionary<string, string> whereDic = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(whereField);
                QueryParam param = new QueryParam() { permission = permission, queryField = queryField, entityName = entityName, ClientWhere = whereDic, orderBy = orderBy };
                if (GetParamValue("tree") == "1")
                {
                    //ColysSharp.DataBase.DBContext.CustomerProperty cp = db.GetParentCustomerProperty(param);
                    //if (string.IsNullOrEmpty(cp.Attr.ForeignTableField)) throw new ClientException(cp.Info.Name + "没有定义ForeignTableField");
                    //string parentField = cp.GetDBFieldName();
                    //string parentVal = GetParamValue("parent");
                    //if (parentVal == "0")
                    //{
                    //    parentVal = "($field is null or $field = 0)";
                    //}
                    //param.ClientWhere.Add(parentField, parentVal);
                    //List<IDBEntity> lst = db.DoQueryEntity(param);
                    //foreach (IDBEntity entity in lst)
                    //{
                    //    SetTreeChildren(db, entity, cp);

                    //}
                    jr.Result = db.QueryTree(param, null);
                }
                else
                {
                    if (!string.IsNullOrEmpty(Request["pageIndex"]))
                    {
                        int pagesize = ColysSharp.Utility.ToInt32(Request["pagesize"], 25);
                        param.Page = new Pagger() { PageIndex = ColysSharp.Utility.ToInt32(Request["pageIndex"]), PageSize = pagesize };
                        jr.Result = db.DoQuery(param);
                        jr.Total = param.Page.Total;
                    }
                    else jr.Result = db.DoQuery(param);
                }
                db.Close();
            }
            catch (Exception ex)
            {
                db.Dispose();
                Utility.LogException(jr, ex);
            } return JsonConvert.SerializeObject(jr);
        }


        //private void SetTreeChildren(DBContext context, IDBEntity entity, ColysSharp.DataBase.DBContext.CustomerProperty cp)
        //{
        //    ColysSharp.DataBase.DBContext.CustomerProperty primaryCodeCP = context.GetPropertyByDBName(cp.Attr.ForeignTableField);
        //   object primaryCode=  primaryCodeCP.Info.GetValue(entity, null);
        //    ITreeDBEntity treeEntity = entity as ITreeDBEntity;
        //    if (treeEntity == null) throw new ClientException(context.GetInstance().Param.entityType.Name + "不是ITreeDBEntity类型，或者查询数据转换为空");
        //    context.GetInstance().Param.ClientWhere[cp.GetDBFieldName()] = primaryCode.ToString();
        //    treeEntity.Children = context.DoQueryEntity(context.GetInstance().Param);
        //    foreach (IDBEntity sub in treeEntity.Children)
        //    {   
        //        SetTreeChildren(context, sub, cp);
        //    }
        //}


        /// <summary>
        /// 查询单个对象
        /// </summary>
        /// <param name="permission">查询权限配置</param>
        /// <param name="entityName">查询的对象实体类名（不需要完整名,Name即可）</param>
        /// <param name="primaryVal">主键的值</param>
        /// <returns></returns>
        public virtual string DoGetOne(string permission, string entityName, string primaryVal)
        {
            JsonMessage jr = new JsonMessage();
            DBContext db = new DBContext(EntityFormat);
            try
            {
                jr.Result = db.DoGetOne(permission, entityName, primaryVal);
                db.Close();
            }
            catch (Exception ex)
            {
                db.Dispose();
                Utility.LogException(jr, ex);
            }
            return JsonConvert.SerializeObject(jr);
        }

        /// <summary>
        /// 保存对象，参数有permission,entityName,entityJson
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public virtual string DoSave()
        {
            string permission = Request["permission"];
            string entityName = Request["entityName"];
            string entityJson = Request["entityJson"];
            JsonMessage jr = new JsonMessage();
            DBContext db = new DBContext(EntityFormat);
            try
            {
                if (entityJson == null) throw new Exception("entityJson is null");
                entityJson = Utility.DecodeBase64(entityJson);
                if (GetParamValue("tree") == "1")
                {

                    IDBEntity entity = db.DeserializeEntity(permission, entityName, entityJson);
                    //获取parent属性的定义
                    ColysSharp.DataBase.DBContext.CustomerProperty cp = db.GetParentCustomerProperty(db.GetInstance().Param);
                    string s = GetParamValue("parent", true);
                    object parentVal = Convert.ChangeType(s, cp.Info.PropertyType);
                    if (parentVal == null) parentVal = 0;
                    cp.Info.SetValue(entity, parentVal, null);

                    //获取parent属性对应的字段ForeignTableField定义
                    DBContext.CustomerProperty primaryCodeCP = db.GetPropertyByDBName(cp.Attr.ForeignTableField);
                    if (!primaryCodeCP.IsPrimaryKey())
                    {
                        //如果parent的值不是对应主键记录，则对code重新编号
                        //获取parent下最大的code
                        string sql = "select max(" + primaryCodeCP.GetDBFieldName() + ") from " + db.GetTableAttr().TableName + " where " + cp.GetDBFieldName() + "='" + parentVal + "' ";
                        DBContext.CustomerProperty markDelCP = db.GetUsaAttr(DBFieldUsage.MarkDelete);
                        if (markDelCP != null) sql += " and " + markDelCP.GetDBFieldName() + " <> -1";
                        object scalar = db.ExecScalar(sql);
                        if (scalar == DBNull.Value)
                        {
                            if (parentVal.Equals(0)) scalar = "00";
                            else scalar = parentVal + "00";
                        }
                        int iscalar = Convert.ToInt32(scalar) + 1;
                        if (scalar.ToString()[0] == '0') scalar = "0" + iscalar;
                        else scalar = iscalar.ToString();
                        primaryCodeCP.Info.SetValue(entity, scalar, null);
                    }
                    db.DoSave(permission, entity);
                    jr.Result = entity.GetPrimaryValue();
                }
                else
                {
                    jr.Result = db.DoSave(permission, entityName, entityJson).GetPrimaryValue();
                }
                db.Close();
            }
            catch (Exception ex)
            {
                Utility.LogException(jr, ex);
            }
            finally
            {
                db.Dispose();
            }
            return JsonConvert.SerializeObject(jr);
        }

        /// <summary>
        /// 删除对象，如果有标记删除则为update，参数有permission,entityName,primaryID
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public virtual string DoDelete()
        {
            string permission = Request["permission"];
            string entityName = Request["entityName"];
            string primaryID = Request["primaryID"];
            DBContext db = new DBContext(EntityFormat);
            JsonMessage jr = new JsonMessage();
            try
            {
                jr.Result = db.DoDelete(permission, entityName, primaryID);
                db.Close();
            }
            catch (Exception ex)
            {
                db.Dispose();
                Utility.LogException(jr, ex);
            }
            return JsonConvert.SerializeObject(jr);
        }

        /// <summary>
        /// 获取客户端IP地址
        /// </summary>
        /// <returns></returns>
        protected string GetClientIp()
        {
            if (System.Web.HttpContext.Current.Request.ServerVariables["HTTP_VIA"] != null)
                return System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"].Split(new char[] { ',' })[0];
            else
                return System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
        }


        /// <summary>
        /// 获取参数值，并转换为指定类型，比如int id = GetParamValue<int>("id");
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="throwNull"></param>
        /// <returns></returns>
        public T GetParamValue<T>(string name, bool throwNull = false) where T : IConvertible
        {
            string s = GetParamValue(name, throwNull);
            return (T)Convert.ChangeType(s, typeof(T));
        }


        /// <summary>
        /// 获取参数值
        /// </summary>
        /// <param name="name">参数名称</param>
        /// <param name="maxLength">最大长度，超过则报异常</param>
        /// <param name="throwNull">如果参数为空是否报异常</param>
        /// <returns></returns>
        public string GetParamValue(string name, int maxLength, bool throwNull = false)
        {
            string str = Request.Params[name];
            if (throwNull)
            {
                if (string.IsNullOrEmpty(str)) throw new ClientException("参数" + name + "值不能为空!");
            }
            if (str != null) if (maxLength > 0 && str.Length > maxLength) throw new ClientException("参数" + name + "长度超过" + maxLength + "!");
            str = str.Replace('\'', ' ').Trim();
            return str;
        }
        /// <summary>
        /// 获取参数值，如果超过255长度会异常
        /// </summary>
        /// <param name="name">参数名称</param>
        /// <param name="throwNull">如果参数为空是否报异常</param>
        /// <returns></returns>
        public string GetParamValue(string name, bool throwNull = false)
        {
            return GetParamValue(name, 255, throwNull);
        }

        /// <summary>
        /// 获取登录用户的ID
        /// </summary>
        /// <returns></returns>
        public int GetUserID()
        {
            if (Request.Cookies["userid"] == null) throw new ClientException("没有登录!");
            return Convert.ToInt32(Request.Cookies["userid"].Value);
        }
        /// <summary>
        /// 获取登录用户的姓名
        /// </summary>
        /// <returns></returns>
        public int GetUserName()
        {
            if (Request.Cookies["userName"] == null) throw new ClientException("没有登录!");
            return Convert.ToInt32(Request.Cookies["userName"].Value);
        }
        /// <summary>
        /// 获取登录用户的头像
        /// </summary>
        /// <returns></returns>
        public int GetUserHeadPicture()
        {
            if (Request.Cookies["userPhoto"] == null) throw new ClientException("没有登录!");
            return Convert.ToInt32(Request.Cookies["userPhoto"].Value);
        }
        /// <summary>
        /// 登录后记录cookie以及相关日志
        /// </summary>
        public void RecordLogin(ColysSharp.Membership.BaseUser userInfo)
        {
            Response.Cookies.Add(new HttpCookie("userid", userInfo.UserId.ToString()));
            Response.Cookies.Add(new HttpCookie("userName", userInfo.Name.ToString()));
            Response.Cookies.Add(new HttpCookie("userPhoto", userInfo.HeadPicture.ToString()));
            Utility.LogMessage("用户" + userInfo.Name + "(" + userInfo.Account + "，" + userInfo.UserId + ") 登录");
        }

        /// <summary>
        /// 如果没有登录，则跳转到登录界面
        /// </summary>
        public void RedirectIfNoLogin()
        {
            if (Request.Cookies["userid"] == null) HttpContext.Response.Redirect(LoginPage);
        }

        public static string LoginPage = "~/Home/Login";
    }


}
