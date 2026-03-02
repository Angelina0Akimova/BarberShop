using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarberShop.AppData
{
    internal class AppConnect
    {
        public static BarbershopDBEntities modelBd;
        public static Users currentUser {  get; set; }
    }
    
}
