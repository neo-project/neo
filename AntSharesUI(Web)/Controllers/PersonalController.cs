using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace AntSharesUI_Web_.Controllers
{
    public class PersonalController : Controller
    {

        // GET: Personal/Cert
        public IActionResult Cert()
        {
            return View();
        }

        // GET: Personal/Protocal
        public IActionResult Protocal()
        {
            return View();
        }
    }
}
