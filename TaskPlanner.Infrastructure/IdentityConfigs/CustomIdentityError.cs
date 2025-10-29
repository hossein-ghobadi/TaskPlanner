using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskPlanner.Infrastructure.IdentityConfigs
{
    public class PersianIdentityError : IdentityErrorDescriber
    {
        public override IdentityError DefaultError()
        {
            return new IdentityError { Code = nameof(DefaultError), Description = "خطا ! خطای ناشناخته" };
        }
        public override IdentityError InvalidEmail(string email)
        {
            return new IdentityError { Code = nameof(InvalidEmail), Description = $"خطا ! ایمیل'{email}' نامعتبر است" };
        }
        public override IdentityError DuplicateEmail(string email)
        {
            return new IdentityError { Code = nameof(DuplicateEmail), Description = $"خطا ! ایمیل '{email}' قبلا در سیستم ثبت شده است" };
        }

        public override IdentityError DuplicateUserName(string username)
        {
            return new IdentityError { Code = nameof(DuplicateUserName), Description = $"خطا ! نام کاربری '{username}' قبلا در سیستم ثبت شده است" };
        }
        public override IdentityError UserAlreadyHasPassword()
        {
            return new IdentityError { Code = nameof(UserAlreadyHasPassword), Description = "خطا ! این گذرواژه در حال حاضر در سیستم ثبت می باشد" };
        }

        public override IdentityError InvalidToken()
        {
            return new IdentityError { Code = nameof(InvalidToken), Description = "خطا ! گذرواژه نا معتبر است" };
        }
        public override IdentityError PasswordMismatch()
        {
            return new IdentityError { Code = nameof(PasswordMismatch), Description = "خطا ! گذرواژه صحیح نمی باشد" };
        }

        public override IdentityError PasswordTooShort(int lenght)
        {
            return new IdentityError { Code = nameof(PasswordTooShort), Description = "خطا ! طول گذرواژه حداقل 8 کاراکتر باید باشد" };
        }

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
        {
            return new IdentityError { Code = nameof(PasswordRequiresUniqueChars), Description = "خطا ! گذرواژه باید حداقل دارای یک کاراکتر غیرتکراری باشد" };
        }

        public override IdentityError PasswordRequiresDigit()
        {
            return new IdentityError { Code = nameof(PasswordRequiresDigit), Description = "خطا ! گذرواژه باید حداقل دارای یک عدد باشد" };
        }

        public override IdentityError PasswordRequiresLower()
        {
            return new IdentityError { Code = nameof(PasswordRequiresLower), Description = "خطا ! گذرواژه باید حداقل دارای یک حرف کوچک انگلیسی باشد" };
        }

        public override IdentityError PasswordRequiresUpper()
        {
            return new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "خطا ! گذرواژه باید حداقل دارای یک حرف بزرگ انگلیسی باشد" };
        }
    }
}
