using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Data.Entity;
using AntSharesUI_Web_.Models;

namespace AntSharesUI_Web_.Controllers
{
    public class WalletController : Controller
    {

        public WalletController(ApplicationDbContext context)
        {
        }

        // GET: Wallet
        public IActionResult Index()
        {
            return View();
        }

        // GET: Wallet/Create
        public IActionResult Create()
        {
            return View();
        }

        // GET: Wallet/Open
        public IActionResult Open()
        {
            return View();
        }

        // POST: Wallet/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Wallet wallet)
        {
            if (ModelState.IsValid)
            {
                
                return RedirectToAction("Index");
            }
            return View(wallet);
        }
    }
}
