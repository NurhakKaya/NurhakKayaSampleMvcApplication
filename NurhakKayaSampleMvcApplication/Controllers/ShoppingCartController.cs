using MvcMusicStore.Models;
using MvcMusicStore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcMusicStore.Controllers
{
    public class ShoppingCartController : Controller
    {
        MusicStoreEntities storeDB = new MusicStoreEntities();
        //
        // GET: /ShoppingCart/

        public ActionResult Index()
        {
            var cart = ShoppingCart.GetCart(this.HttpContext);

            var viewModel = new ShoppingCartViewModel
            {
                CartItems = cart.GetCartItems(),
                CartTotal = cart.GetTotal()
            };

            return View(viewModel);
        }

        //
        // GET: /Store/AddToCart/5
        public ActionResult AddtoCart(int id)
        {
            var addedAlbum = storeDB.Albums
                .Single(album => album.AlbumId == id);

            var cart = ShoppingCart.GetCart(this.HttpContext);
            cart.AddToCart(addedAlbum);

            return RedirectToAction("Index");
        }

        //
        // AJAX: /ShoppingCart/RemoveFromCart/5 
        [HttpPost]
        public ActionResult RemoveFromCart(int id)
        {
            var cart = ShoppingCart.GetCart(this.HttpContext);

            //Kullanıcıya doğrulama için sepetten çıkarılan albümün adı alınıyor
            string albumName = storeDB.Carts
                .Single(item => item.RecordId == id).Album.Title;

            //Albüm sepetten çıkarılıyor
            int itemCount = cart.RemoveFromCart(id);

            //İşlem sonrası mesajı ekrana basmak için JSON nesnesi içeriği dolduruluyor
            var results = new ShoppingCartRemoveViewModel
            {
                Message = Server.HtmlEncode(albumName) +
                    "has been removed from your shopping cart.",
                CartTotal = cart.GetTotal(),
                CartCount = cart.GetCount(),
                ItemCount = itemCount,
                DeleteId = id
            };

            return Json(results);
        }

        //
        // GET: /ShoppingCart/CartSummary
        // ChildActionOnly attribute yapısı ile bu action'ın sadece bir partial view içinden çağrılmasını sağlamış oluyoruz. Bu sayede tarayıcı üzerinden bu şekilde action çağrılamaz:  /ShoppingCart/GenreMenu
        [ChildActionOnly]
        public ActionResult CartSummary()
        {
            var cart = ShoppingCart.GetCart(this.HttpContext);

            ViewData["CartCount"] = cart.GetCount();

            return PartialView("CartSummary");
        }

    }
}
