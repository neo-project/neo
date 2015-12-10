using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AntSharesUI_Web_.Models
{
    public class Wallet
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "必须填写钱包名称")]
        [Display(Name = "钱包名称")]
        public string Name { get; set; }

        [Required(ErrorMessage = "必须填写钱包密码")]
        [Display(Name = "钱包密码")]
        public string Password { get; set; }

        [Required(ErrorMessage = "必须填写钱包确认密码")]
        [Display(Name = "钱包确认密码")]
        [Compare("Password", ErrorMessage = "密码和确认密码不匹配。")]
        public string ConfirmPassword { get; set; }
        
    }
}
