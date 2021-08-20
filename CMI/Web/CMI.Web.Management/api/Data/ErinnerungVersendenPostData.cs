using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CMI.Web.Management.api.Data
{
    public class ErinnerungVersendenPostData
    {
        public List<int> OrderItemIds { get; set; }
        public string Language { get; set; }
    }
}