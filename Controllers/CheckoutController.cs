﻿using ECommerceWeb.Helpers;
using ECommerceWeb.Interface;
using ECommerceWeb.Models;
using ECommerceWeb.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerceWeb.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ICartService cartService;
        private readonly ICheckoutService checkoutService;
        private readonly ICouponService couponService;

        public CheckoutController(ICartService _cartService, ICheckoutService _checkoutService, ICouponService _couponService)
        {
            cartService = _cartService;
            checkoutService = _checkoutService;
            couponService = _couponService;
        }
        public ActionResult Checkout()
        {
            return View(new CheckoutViewModel());
        }

        public async Task<ActionResult> GetCartProduct()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            CheckoutViewModel checkoutView = new CheckoutViewModel();
            checkoutView.Cart = await cartService.GetCartByUserId(userId);
            checkoutView.NoOfCartItems = checkoutView.Cart.Count();
            return PartialView("_CartProducts", checkoutView);
        }

        public async Task<ActionResult> ValidateCouponCode(string code)
        {
            var Coupons = couponService.GetCoupon().FirstOrDefault(m => m.CouponCode == code);
            if (Coupons != null)
            {
                var Product = couponService.CouponProducts(Coupons.CouponId).ToList();
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var Cart = await cartService.GetCartByUserId(userId);
                var model = new
                {
                    Coupon = Coupons,
                    cartitem = Cart.Where(x => Product.Contains(x.Products.ProductId))
                };
                return Json(model); // 1 for validate and 0 for not
            }
            return Json(null);
        }

        [HttpPost]
        public async Task<ActionResult> Checkout(CheckoutViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.UserId = userId;
            var Id = await checkoutService.PlaceOrder(model);
            SessionHelper.SetObjectAsJson(HttpContext.Session, "cart", new List<CartItem>());
            return RedirectToAction("Payment", new Order { OrderId = Id});
        }
        public ActionResult Payment(Order model)
        {
            return View(model);
        }
        public ActionResult Success(string orderid, string payment_intent,string redirect_status)
        {
            //?payment_intent=pi_3NIpHYSHpKdU6w510PuP6zaB 
            //payment_intent_client_secret=pi_3NIpHYSHpKdU6w510PuP6zaB_secret_UI9JhlEDrNsKHieOXUZ5cCfYa
            //redirect_status=succeeded
            if (redirect_status == "succeeded")
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                checkoutService.UpdatePayment(payment_intent,redirect_status, orderid);
            }
            return View();
        }
    }
}