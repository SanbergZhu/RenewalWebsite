﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Models
{
    public class CustomerPaymentViewModel
    {
        public string UserName { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [CreditCard]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; }

        [Required]
        [Display(Name = "Security Code")]
        [RegularExpression(@"\d{3}", ErrorMessage = "Invalid CVC number")]
        public string Cvc { get; set; }

        [Display(Name = "Expiration Date")]
        [Range(1, 12, ErrorMessage = "Invalid month")]
        public int ExpiryMonth { get; set; }

        [Range(17, 30, ErrorMessage = "Invalid year")]
        public int ExpiryYear { get; set; }

        // Donation attributes
        public int DonationId { get; set; }
        public string CycleId { get; set; }

        public List<CustomerSubscriptionViewModel> Subscriptions { get; set; }

        // Address

        [Display(Name = "Address Line 1")]
        public string AddressLine1 { get; set; }

        [Display(Name = "Address Line 2")]
        public string AddressLine2 { get; set; }

        [Display(Name = "State/Province")]
        public string State { get; set; }

        [Display(Name = "Zip/Postal Code")]
        public string Zip { get; set; }

        [Display(Name = "City")]
        public string City { get; set; }

        [Display(Name = "Country")]
        public string Country { get; set; }

        public string Frequency { get; set; }

        public string Description { get; set; }

        public decimal Amount { get; set; }

        //[Required]
        //public string Currency { get; set; }

        public string Paymentgatway { get; set; }

        public string DisableCurrencySelection { get; set; }

        public decimal ExchangeRate { get; set; }

        public bool IsCustom { get; set; }
    }
}
