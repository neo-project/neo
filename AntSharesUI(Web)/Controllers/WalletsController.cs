using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Data.Entity;
using AntSharesUI_Web_.Models;
using System.Web;
using System.IO;

namespace AntSharesUI_Web_.Controllers
{
    public class WalletController : Controller
    {

        public WalletController(ApplicationDbContext context)
        {

        }

        // GET: Wallet
        public IActionResult MyWallet()
        {
            ViewBag.Addresses = new string[] { "AbcdXweW6trsVwcBSUYrK69u4cBhDg4ZJF", "ANttyFkVZxGh93TmjgkiFe1TgS3csGfhJD", "ANT7ciAPanUuKHSvUoQZd2TXkKTFfCwrj5" };
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

        // GET: Wallet/ChangePassword
        public IActionResult ChangePassword()
        {
            return View();
        }

        // GET: Wallet/Address
        public IActionResult Address(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

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

        //[HttpPost]
        //public ActionResult Upload(HttpPostedFileBase wallet_file)
        //{
        //    string path = @"D:\Temp\";

        //    if (wallet_file != null)
        //        wallet_file.SaveAs(path + wallet_file.FileName);

        //    return RedirectToAction("Index");
        //}

        // POST: Wallet/Open
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Open(Wallet wallet)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            return View(wallet);
        }

        // POST: Wallet/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(Wallet wallet)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            return View(wallet);
        }
    }
}
