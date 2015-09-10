using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColysSharp.DataBase;

namespace ColysSharp.Membership
{
    [DBTable(TableName = "A0_Users")]
    public class BaseUser : IDBEntity
    {
        [DBField(Usage = DBFieldUsage.PrimaryKey)]
        public int UserId { get; set; }
        public string Name { get; set; }
        public string HeadPicture { get; set; }

        public string Account { get; set; }

        public string Mobile { get; set; }
        public string Email { get; set; }

        public string Password { get; set; }

        public string Birthday { get; set; }
        public string IDCard { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
