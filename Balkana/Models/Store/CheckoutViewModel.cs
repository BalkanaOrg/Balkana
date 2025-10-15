using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Balkana.Data.Models.Store;

namespace Balkana.Models.Store
{
    public class CheckoutViewModel
    {
        // Cart summary (not validated - loaded in controller)
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public ShoppingCartViewModel Cart { get; set; }
        
        // Customer info (for guest checkout)
        public bool IsGuestCheckout { get; set; }
        
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        [Required]
        [Display(Name = "First Name")]
        [StringLength(100)]
        public string FirstName { get; set; }
        
        [Required]
        [Display(Name = "Last Name")]
        [StringLength(100)]
        public string LastName { get; set; }
        
        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; }
        
        // Shipping address
        [Required]
        [Display(Name = "Address")]
        [StringLength(200)]
        public string ShippingAddress { get; set; }
        
        [Required]
        [Display(Name = "City")]
        [StringLength(100)]
        public string ShippingCity { get; set; }
        
        [Display(Name = "Postal Code")]
        [StringLength(20)]
        public string ShippingPostalCode { get; set; }
        
        [Required]
        [Display(Name = "Country")]
        public string ShippingCountry { get; set; } = "Bulgaria";
        
        // Delivery
        [Required]
        [Display(Name = "Delivery Method")]
        public DeliveryProvider DeliveryProvider { get; set; }
        
        [Display(Name = "Delivery Office (for office pickup)")]
        public string? DeliveryOfficeCode { get; set; }
        
        public string? DeliveryOfficeAddress { get; set; }
        
        // Payment
        [Required]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; }
        
        // Billing same as shipping?
        public bool BillingSameAsShipping { get; set; } = true;
        
        // Billing address (not required - only if BillingSameAsShipping is false)
        [Display(Name = "Billing Address")]
        public string? BillingAddress { get; set; }
        
        [Display(Name = "Billing City")]
        public string? BillingCity { get; set; }
        
        [Display(Name = "Billing Postal Code")]
        public string? BillingPostalCode { get; set; }
        
        [Display(Name = "Billing Country")]
        public string? BillingCountry { get; set; }
        
        // Notes
        [Display(Name = "Order Notes (Optional)")]
        [StringLength(1000)]
        public string CustomerNotes { get; set; }
        
        // Calculated costs
        public decimal ShippingCost { get; set; }
        public decimal Tax { get; set; }
        
        // Dropdowns
        public List<SelectListItem> DeliveryMethods { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PaymentMethods { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> EkontOffices { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> SpeedyOffices { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Countries { get; set; } = new List<SelectListItem>();
        
        // Terms acceptance
        [Required]
        [Display(Name = "I agree to the terms and conditions")]
        public bool AcceptTerms { get; set; }
    }
}

