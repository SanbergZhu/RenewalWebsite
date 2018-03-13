﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RenewalWebsite.Models;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.ViewComponents
{
    public class SubscriptionsViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOptions<StripeSettings> _stripeSettings;

        public SubscriptionsViewComponent(
            UserManager<ApplicationUser> userManager,
            IOptions<StripeSettings> stripeSettings)
        {
            _userManager = userManager;
            _stripeSettings = stripeSettings;
        }

        public IViewComponentResult Invoke(int numberOfItems)
        {
            var user = GetCurrentUserAsync();

            if (!string.IsNullOrEmpty(user.StripeCustomerId))
            {
                var customerService = new StripeSubscriptionService(_stripeSettings.Value.SecretKey);

                var StripSubscriptionListOption = new StripeSubscriptionListOptions()
                {
                    CustomerId = user.StripeCustomerId,
                    Limit = 100
                };

                var customerSubscription = new CustomerPaymentViewModel();

                try
                {
                    var subscriptions = customerService.List(StripSubscriptionListOption);
                    customerSubscription = new CustomerPaymentViewModel
                    {
                        UserName = user.Email,
                        Subscriptions = subscriptions.Select(s => new CustomerSubscriptionViewModel
                        {
                            Id = s.Id,
                            Name = s.StripePlan.Id,
                            Amount = s.StripePlan.Amount,
                            Currency = s.StripePlan.Currency,
                            Status = s.Status
                        }).ToList()
                    };
                }
                catch (StripeException sex)
                {
                    ModelState.AddModelError("CustmoerNotFound", sex.Message);
                }

                return View("View", customerSubscription);
            }

            var subscription = new CustomerPaymentViewModel
            {
                UserName = user.Email,

                Subscriptions = new List<CustomerSubscriptionViewModel>()
            };
            return View("View", subscription);
        }

        private ApplicationUser GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User).Result;
        }
    }
}
