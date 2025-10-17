using ReverseMarket.Models.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ReverseMarket.Models
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [StringLength(50)]
        [Display(Name = "الاسم الأول")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "الاسم الأخير مطلوب")]
        [StringLength(50)]
        [Display(Name = "الاسم الأخير")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        [Display(Name = "اسم المستخدم")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        [Display(Name = "رقم الهاتف")]
        public string PhoneNumber { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; }

        [Required(ErrorMessage = "نوع المستخدم مطلوب")]
        [Display(Name = "نوع المستخدم")]
        public UserType UserType { get; set; }

        [Required(ErrorMessage = "المدينة مطلوبة")]
        [StringLength(100)]
        [Display(Name = "المدينة")]
        public string City { get; set; }

        [Required(ErrorMessage = "الحي مطلوب")]
        [StringLength(100)]
        [Display(Name = "الحي")]
        public string District { get; set; }

        [StringLength(255)]
        [Display(Name = "الموقع")]
        public string Location { get; set; }

        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "الجنس مطلوب")]
        [StringLength(10)]
        [Display(Name = "الجنس")]
        public string Gender { get; set; }

        [StringLength(255)]
        [Display(Name = "اسم المتجر")]
        public string StoreName { get; set; }

        [StringLength(1000)]
        [Display(Name = "وصف المتجر")]
        public string StoreDescription { get; set; }

        [StringLength(500)]
        [Display(Name = "رابط الموقع 1")]
        [Url(ErrorMessage = "الرابط غير صحيح")]
        public string WebsiteUrl1 { get; set; }

        [StringLength(500)]
        [Display(Name = "رابط الموقع 2")]
        [Url(ErrorMessage = "الرابط غير صحيح")]
        public string WebsiteUrl2 { get; set; }

        [StringLength(500)]
        [Display(Name = "رابط الموقع 3")]
        [Url(ErrorMessage = "الرابط غير صحيح")]
        public string WebsiteUrl3 { get; set; }

        [Display(Name = "الهاتف موثق")]
        public bool IsPhoneVerified { get; set; }

        [Display(Name = "البريد موثق")]
        public bool IsEmailVerified { get; set; }

        [Display(Name = "المتجر موافق عليه")]
        public bool IsStoreApproved { get; set; }

        [Display(Name = "فئات المتجر")]
        public List<int> StoreCategories { get; set; }

        [Display(Name = "الفئات الحالية")]
        public List<string> CurrentStoreCategories { get; set; }

        [Display(Name = "الأدوار")]
        public List<string> SelectedRoles { get; set; }

        public List<string> AvailableRoles { get; set; }
    }
}