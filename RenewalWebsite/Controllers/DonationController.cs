﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RenewalWebsite.Services;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using RenewalWebsite.Models;
using Stripe;
using RenewalWebsite.Helpers;

namespace RenewalWebsite.Controllers
{
    [Authorize]
    public class DonationController : Controller
    {
        private const string DonationCaption = "Renewal Center donation";
        private readonly IDonationService _donationService;
        private readonly IOptions<StripeSettings> _stripeSettings;
        private readonly UserManager<ApplicationUser> _userManager;

        public DonationController(
            UserManager<ApplicationUser> userManager,
            IDonationService donationService,
            IOptions<StripeSettings> stripeSettings)
        {
            _userManager = userManager;
            _donationService = donationService;
            _stripeSettings = stripeSettings;
        }

        [Route("Donation/Payment")]
        [HttpGet]
        public ActionResult payment()
        {
            return RedirectToAction("index", "home");
        }

        [Route("Donation/Payment/{id}")]
        public async Task<IActionResult> Payment(int id)
        {
            var user = await GetCurrentUserAsync();
            var donation = _donationService.GetById(id);
            var detail = (DonationViewModel)donation;
            detail.DonationOptions = _donationService.DonationOptions;

            // Check for existing customer
            // edit = 1 means user wants to edit the credit card information
            if (!string.IsNullOrEmpty(user.StripeCustomerId))
            {
                try
                {
                    var CustomerService = new StripeCustomerService(_stripeSettings.Value.SecretKey);
                    StripeCustomer objStripeCustomer = CustomerService.Get(user.StripeCustomerId);
                    StripeCard objStripeCard = null;

                    if (objStripeCustomer.Sources != null && objStripeCustomer.Sources.TotalCount > 0 && objStripeCustomer.Sources.Data.Any())
                    {
                        objStripeCard = objStripeCustomer.Sources.Data.FirstOrDefault().Card;
                    }

                    if (objStripeCard != null && !string.IsNullOrEmpty(objStripeCard.Id))
                    {
                        var objCustomerRePaymentViewModel = new CustomerRePaymentViewModel
                        {
                            Name = user.FullName,
                            AddressLine1 = user.AddressLine1,
                            AddressLine2 = user.AddressLine2,
                            City = user.City,
                            State = user.State,
                            Country = user.Country,
                            Zip = user.Zip,
                            DonationId = donation.Id,
                            Description = detail.GetDescription(),
                            Frequency = detail.GetCycle(),
                            Amount = detail.GetDisplayAmount(),
                            Last4Digit = objStripeCard.Last4,
                            CardId = objStripeCard.Id,
                            Currency = (objStripeCustomer.Currency + "").ToUpper(),
                            DisableCurrencySelection = string.IsNullOrEmpty(objStripeCustomer.Currency) ? "0" : "1"

                        };

                        return View("RePayment", objCustomerRePaymentViewModel);
                    }
                }
                catch (StripeException sex)
                {
                    ModelState.AddModelError("CustomerNotFound", sex.Message);
                }
            }

            var model = new CustomerPaymentViewModel
            {
                Name = user.FullName,
                AddressLine1 = user.AddressLine1,
                AddressLine2 = user.AddressLine2,
                City = user.City,
                State = user.State,
                Country = string.IsNullOrEmpty(user.Country) ? "US" : user.Country,
                Zip = user.Zip,
                DonationId = donation.Id,
                Description = detail.GetDescription(),
                Frequency = detail.GetCycle(),
                Amount = detail.GetDisplayAmount(),
                Currency = "USD"
            };

            return View("Payment", model);
        }

        [HttpPost]
        public async Task<IActionResult> Payment(CustomerPaymentViewModel payment)
        {
            try
            {
                var user = await GetCurrentUserAsync();

                if (!ModelState.IsValid)
                {
                    return View(payment);
                }

                var customerService = new StripeCustomerService(_stripeSettings.Value.SecretKey);
                var donation = _donationService.GetById(payment.DonationId);

                // Construct payment
                if (string.IsNullOrEmpty(user.StripeCustomerId))
                {
                    var customer = new StripeCustomerCreateOptions
                    {
                        Email = user.Email,
                        Description = $"{user.Email} {user.Id}",
                        SourceCard = new SourceCard
                        {
                            Name = payment.Name,
                            Number = payment.CardNumber,
                            Cvc = payment.Cvc,
                            ExpirationMonth = payment.ExpiryMonth,
                            ExpirationYear = payment.ExpiryYear,
                            StatementDescriptor = _stripeSettings.Value.StatementDescriptor,
                            Description = DonationCaption,
                            AddressLine1 = payment.AddressLine1,
                            AddressLine2 = payment.AddressLine2,
                            AddressCity = payment.City,
                            AddressState = payment.State,
                            AddressCountry = payment.Country,
                            AddressZip = payment.Zip
                        }
                    };
                    var stripeCustomer = customerService.Create(customer);
                    user.StripeCustomerId = stripeCustomer.Id;
                }
                else
                {
                    //Check for existing credit card, if new credit card number is same as exiting credit card then we delete the existing
                    //Credit card information so new card gets generated automatically as default card.
                    try
                    {
                        var ExistingCustomer = customerService.Get(user.StripeCustomerId);
                        if (ExistingCustomer.Sources != null && ExistingCustomer.Sources.TotalCount > 0 && ExistingCustomer.Sources.Data.Any())
                        {
                            var cardService = new StripeCardService(_stripeSettings.Value.SecretKey);
                            foreach (var cardSource in ExistingCustomer.Sources.Data)
                            {
                                cardService.Delete(user.StripeCustomerId, cardSource.Card.Id);
                            }
                        }
                    }
                    catch (Exception exSub)
                    {
                        return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = exSub.Message });
                    }

                    var customer = new StripeCustomerUpdateOptions
                    {
                        SourceCard = new SourceCard
                        {
                            Name = payment.Name,
                            Number = payment.CardNumber,
                            Cvc = payment.Cvc,
                            ExpirationMonth = payment.ExpiryMonth,
                            ExpirationYear = payment.ExpiryYear,
                            StatementDescriptor = _stripeSettings.Value.StatementDescriptor,
                            Description = DonationCaption,
                            AddressLine1 = payment.AddressLine1,
                            AddressLine2 = payment.AddressLine2,
                            AddressCity = payment.City,
                            AddressState = payment.State,
                            AddressCountry = payment.Country,
                            AddressZip = payment.Zip
                        }
                    };

                    var stripeCustomer = customerService.Update(user.StripeCustomerId, customer);
                    user.StripeCustomerId = stripeCustomer.Id;
                }

                user.FullName = payment.Name;
                user.AddressLine1 = payment.AddressLine1;
                user.AddressLine2 = payment.AddressLine2;
                user.City = payment.City;
                user.State = payment.State;
                user.Country = payment.Country;
                user.Zip = payment.Zip;
                await _userManager.UpdateAsync(user);


                // Add customer to Stripe
                if (EnumInfo<PaymentCycle>.GetValue(donation.CycleId) == PaymentCycle.OneOff)
                {
                    var model = (DonationViewModel)donation;
                    model.DonationOptions = _donationService.DonationOptions;

                    var charges = new StripeChargeService(_stripeSettings.Value.SecretKey);

                    // Charge the customer
                    var charge = charges.Create(new StripeChargeCreateOptions
                    {
                        Amount = model.GetAmount(),
                        Description = DonationCaption,
                        Currency = payment.Currency.ToLower(),
                        CustomerId = user.StripeCustomerId,
                        //ReceiptEmail = user.Email,
                        StatementDescriptor = _stripeSettings.Value.StatementDescriptor,
                    });

                    if (charge.Paid)
                    {
                        var completedMessage = new CompletedViewModel
                        {
                            Message = $"Your card was charged successfully. Thank you for your kind gift of ${model.GetDisplayAmount()}.",
                            HasSubscriptions = false
                        };
                        return RedirectToAction("Thanks", completedMessage);
                        //return View("Thanks", completedMessage);
                    }
                    //return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = "Error" });
                    return View("Error");
                }

                // Add to existing subscriptions and charge 
                donation.currency = payment.Currency;
                var plan = _donationService.GetOrCreatePlan(donation);

                var subscriptionService = new StripeSubscriptionService(_stripeSettings.Value.SecretKey);
                var result = subscriptionService.Create(user.StripeCustomerId, plan.Id);
                if (result != null)
                {
                    var completedMessage = new CompletedViewModel
                    {
                        Message = $"Your gift will repeat {result.StripePlan.Name.Split("_")[0]}.To manage or cancel your subscription anytime, follow the link below.",
                        HasSubscriptions = true
                    };
                    return RedirectToAction("Thanks", completedMessage);
                    //return View("Thanks", completedMessage);
                }
            }
            catch (StripeException sex)
            {
                if (sex.Message.ToLower().Contains("customer"))
                {
                    return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = sex.Message });
                }
                else
                {
                    ModelState.AddModelError("error", sex.Message);
                    return View(payment);
                }
            }
            catch (Exception ex)
            {
                //return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = ex.Message });
                return View("Error");
            }
            //return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = "Error" });
            return View("Error");
        }

        [Route("Donation/Payment/{id}/{edit?}")]
        public async Task<IActionResult> Payment(int id, int edit)
        {
            var user = await GetCurrentUserAsync();
            var donation = _donationService.GetById(id);
            var detail = (DonationViewModel)donation;
            detail.DonationOptions = _donationService.DonationOptions;
            CustomerPaymentViewModel model = new CustomerPaymentViewModel();

            try
            {
                var customerService = new StripeCustomerService(_stripeSettings.Value.SecretKey);
                var ExistingCustomer = customerService.Get(user.StripeCustomerId);
                model = new CustomerPaymentViewModel
                {
                    Name = user.FullName,
                    AddressLine1 = user.AddressLine1,
                    AddressLine2 = user.AddressLine2,
                    City = user.City,
                    State = user.State,
                    Country = user.Country,
                    Zip = user.Zip,
                    DonationId = donation.Id,
                    Description = detail.GetDescription(),
                    Frequency = detail.GetCycle(),
                    Amount = detail.GetDisplayAmount(),
                    Currency = string.IsNullOrEmpty(ExistingCustomer.Currency) ? string.Empty : ExistingCustomer.Currency.ToUpper(),
                    DisableCurrencySelection = "1" // Disable currency selection for already created customer as stripe only allow same currency for one customer
                };
            }
            catch (StripeException sex)
            {
                ModelState.AddModelError("CustomerNotFound", sex.Message);
            }

            return View("Payment", model);
        }

        [HttpPost]
        public async Task<IActionResult> RePayment(CustomerRePaymentViewModel payment)
        {
            try
            {
                var user = await GetCurrentUserAsync();

                if (!ModelState.IsValid)
                {
                    return View(payment);
                }

                var customerService = new StripeCustomerService(_stripeSettings.Value.SecretKey);
                var donation = _donationService.GetById(payment.DonationId);

                user.FullName = payment.Name;
                user.AddressLine1 = payment.AddressLine1;
                user.AddressLine2 = payment.AddressLine2;
                user.City = payment.City;
                user.State = payment.State;
                user.Country = payment.Country;
                user.Zip = payment.Zip;
                await _userManager.UpdateAsync(user);

                // Add customer to Stripe
                if (EnumInfo<PaymentCycle>.GetValue(donation.CycleId) == PaymentCycle.OneOff)
                {
                    var model = (DonationViewModel)donation;
                    model.DonationOptions = _donationService.DonationOptions;

                    var charges = new StripeChargeService(_stripeSettings.Value.SecretKey);

                    // Charge the customer
                    var charge = charges.Create(new StripeChargeCreateOptions
                    {
                        Amount = model.GetAmount(),
                        Description = DonationCaption,
                        Currency = payment.Currency.ToLower(),
                        CustomerId = user.StripeCustomerId,
                        //ReceiptEmail = user.Email,
                        StatementDescriptor = _stripeSettings.Value.StatementDescriptor,
                    });

                    if (charge.Paid)
                    {
                        var completedMessage = new CompletedViewModel
                        {
                            Message = $"Your card was charged successfully. Thank you for your kind gift of ${model.GetDisplayAmount()}.",
                            HasSubscriptions = false
                        };
                        return RedirectToAction("Thanks", completedMessage);
                        //return View("Thanks", completedMessage);
                    }
                    //return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = "Error" });
                    return View("Error");
                }
                donation.currency = payment.Currency;
                // Add to existing subscriptions and charge 
                var plan = _donationService.GetOrCreatePlan(donation);

                var subscriptionService = new StripeSubscriptionService(_stripeSettings.Value.SecretKey);
                var result = subscriptionService.Create(user.StripeCustomerId, plan.Id);
                if (result != null)
                {
                    var completedMessage = new CompletedViewModel
                    {
                        Message = $"Your gift will repeat {result.StripePlan.Name.Split("_")[0]}.To manage or cancel your subscription anytime, follow the link below.",
                        HasSubscriptions = true
                    };
                    return RedirectToAction("Thanks", completedMessage);
                    //return View("Thanks", completedMessage);
                }
            }
            catch (StripeException sex)
            {
                if (sex.Message.ToLower().Contains("customer"))
                {
                    return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = sex.Message });
                }
                else
                {
                    ModelState.AddModelError("error", sex.Message);
                    return View(payment);
                }
            }
            catch (Exception ex)
            {
                //return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = "Error" });
                return View("Error");
            }
            //return RedirectToAction("Error", "Error", new ErrorViewModel() { Error = "Error" });
            return View("Error");
        }

        private Task<ApplicationUser> GetCurrentUserAsync()
        {
            return _userManager.GetUserAsync(HttpContext.User);
            //Source src = new Source();
            //src.Type = SourceType.
        }

        public ActionResult Thanks(CompletedViewModel model)
        {
            return View(model);
        }
    }
}