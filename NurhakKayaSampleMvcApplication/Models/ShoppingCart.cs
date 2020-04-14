using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MvcMusicStore.Models
{
    public partial class ShoppingCart
    {
        MusicStoreEntities storeDB = new MusicStoreEntities();

        string ShoppingCardId { get; set; }

        public const string CartSessionKey = "CartId";

        public static ShoppingCart GetCart(HttpContextBase context)
        {
            var cart = new ShoppingCart();
            cart.ShoppingCardId = cart.GetCartId(context);
            return cart;
        }

        public static ShoppingCart GetCart(Controller controller)
        {
            return GetCart(controller.HttpContext);
        }

        public void AddToCart(Album album)
        {
            var cartItem = storeDB.Carts.SingleOrDefault(
                c => c.CartId == ShoppingCardId
                && c.AlbumId == album.AlbumId);

            if (cartItem == null)
            {
                cartItem = new Cart
                {
                    AlbumId = album.AlbumId,
                    CartId = ShoppingCardId,
                    Count = 1,
                    DateCreated = DateTime.Now
                };

                storeDB.Carts.Add(cartItem);
            }
            else
            {
                cartItem.Count++;
            }

            storeDB.SaveChanges();
        }

        public int RemoveFromCart(int id)
        {
            var cartItem = storeDB.Carts.Single(
                    cart => cart.CartId == ShoppingCardId
                    && cart.RecordId == id);

            int itemCount = 0;

            if (cartItem!=null)
            {
                if (cartItem.Count>1)
                {
                    cartItem.Count--;
                    itemCount = cartItem.Count;
                }
                else
                {
                    storeDB.Carts.Remove(cartItem);
                }

                storeDB.SaveChanges();
            }

            return itemCount;
        }

        public void EmptyCart()
        {
            var cartItems = storeDB.Carts.Where(cart => cart.CartId == ShoppingCardId);

            foreach (var cartItem in cartItems)
            {
                storeDB.Carts.Remove(cartItem);
            }

            storeDB.SaveChanges();
        }

        public List<Cart> GetCartItems()
        {
            return storeDB.Carts.Where(cart => cart.CartId == ShoppingCardId).ToList();
        }

        public int GetCount()
        {
            int? count = (from cartItems in storeDB.Carts
                          where cartItems.CartId == ShoppingCardId
                          select (int?)cartItems.Count).Sum();

            //Tüm girişler null ise, adet değeri 0 olarak döndürülür
            return count ?? 0;
        }

        public decimal GetTotal()
        {
            decimal? total = (from cartItems in storeDB.Carts
                              where cartItems.CartId == ShoppingCardId
                              select (int?)cartItems.Count * cartItems.Album.Price).Sum();

            //Toplam değer null ise, değer 0 olarak döndürülür
            return total ?? decimal.Zero;
        }

        public int CreateOrder(Order order)
        {
            decimal orderTotal = 0;

            var cartItems = GetCartItems();

            foreach (var item in cartItems)
            {
                var orderDetail = new OrderDetail
                {
                    AlbumId=item.AlbumId,
                    OrderId=order.OrderId,
                    UnitPrice=item.Album.Price,
                    Quantity=item.Count
                };

                orderTotal += (item.Count * item.Album.Price);

                storeDB.OrderDetails.Add(orderDetail);
            }

            order.Total = orderTotal;
            storeDB.SaveChanges();
            EmptyCart();

            //İşlemin tamamlandığını göstermek için sipariş numarası döndürülüyor
            return order.OrderId;
        }

        //Kullanıcıya ait alışveriş sepeti ile ilgili bilgilerin tutulduğu cookie'lere erişim için HttpContextBase yapısı kullanılıyor
        public string GetCartId(HttpContextBase context)
        {
            if (context.Session[CartSessionKey]==null)
            {
                if (!string.IsNullOrWhiteSpace(context.User.Identity.Name))
                {
                    context.Session[CartSessionKey] = context.User.Identity.Name;
                }
                else
                {
                    Guid tempCartId = Guid.NewGuid();
                    //tempCartId kullanıcı tarafına bir cookie olarak gönderiliyor
                    context.Session[CartSessionKey] = tempCartId.ToString();
                }
            }

            return context.Session[CartSessionKey].ToString();
        }

        //Bir kullanıcı giriş yaptığında, alışveriş sepeti bu kullanıcının kullanıcı adı ile ilişkilendiriliyor
        public void MigrateCart(string userName)
        {
            var shoppingCart = storeDB.Carts.Where(c => c.CartId == ShoppingCardId);
            
            foreach (Cart item in shoppingCart)
            {
                item.CartId = userName;
            }
            storeDB.SaveChanges();
        }
    }
}