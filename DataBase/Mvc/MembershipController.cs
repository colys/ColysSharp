using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using ColysSharp.DataBase;
using ColysSharp.Modals;
using Newtonsoft.Json;

namespace ColysSharp.Mvc
{
    public class MembershipController:BaseController
    {
        protected override string EntityFormat { get { return "ColysSharp.Membership.{0},ColysSharp.DataBase"; } }

        [HttpPost]
        public string SaveUser(string json) {
            string permission = "membsership-saveuser";
            string entityName = "User";
            string entityJson = Request["entityJson"];
            JsonMessage jr = new JsonMessage();
            DBContext db = new DBContext(EntityFormat);
            try
            {
                if (entityJson == null) throw new Exception("entityJson is null");
                entityJson = Server.UrlDecode(entityJson);
                jr.Result = db.DoSave(permission, entityName, entityJson).GetPrimaryValue();
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

        public string GetUser(int id)
        {
            string permission = "membsership-saveuser";
            return base.DoQuery(permission, "*", "User", "{ID:" + id + "}", null);
        }

        
    }
}
