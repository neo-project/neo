using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace AntSharesUI_Web_.Controllers
{
    public class PersonalController : Controller
    {

        // GET: Index
        public IActionResult Index()
        {
            return View();
        }

        // GET: Cert
        public IActionResult Cert()
        {
            return View();
        }

        // GET: Protocal
        public IActionResult Protocal()
        {
            return View();
        }
    }
}
