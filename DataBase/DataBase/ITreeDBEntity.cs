using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ColysSharp.DataBase
{
    public class ITreeDBEntity:IDBEntity
    {
        public List<IDBEntity> Children = new List<IDBEntity>();
    }
}
