using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using jobsite.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace jobsite.Areas.Identity.Pages.Account.Manage
{
    public class CVModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public CVModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }
        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [Display(Name = "CV")]
        [BindProperty]
        public IFormFile FormFile { get; set; }

        public string CV { get; set; }
        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            Username = userName;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (((Candidate)user).CV != null)
            {
                CV = ((Candidate)user).CV.Title;
                //var stream = new MemoryStream(((Candidate)user).CV.Content);
                //FormFile = new FormFile(stream, 0, stream.Length, ((Candidate)user).CV.Title, ((Candidate)user).CV.Title);
            }
            else
            {
                CV = null;
            }
            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            bool cvUploaded = (FormFile != null);

            byte[] Content = new byte[0];

            if (cvUploaded)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await FormFile.CopyToAsync(memoryStream);

                    if (memoryStream.Length < 2097152 * 4)
                    {

                        Content = memoryStream.ToArray();
                    }
                    else
                    {
                        ModelState.AddModelError("File", "The file is too large.");
                    }
                }
            }


            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            if (cvUploaded)
            {
                CV cv = new CV();
                cv.Title = FormFile.FileName;
                cv.Content = Content;
                cv.Extension = Path.GetExtension(FormFile.FileName);
                if (cv.Extension.Length > 20)
                {
                    cv.Extension = "unknown";
                }

                ((Candidate)user).CV = cv;
                var updt = await _userManager.UpdateAsync(user);

                if (!updt.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to update educations.";
                    return RedirectToPage();
                }
                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Your profile has been updated";
            }
            
            return RedirectToPage();

        }

        public async Task<IActionResult> OnGetDownloadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (((Candidate)user).CV != null)
            {
                var stream = new MemoryStream(((Candidate)user).CV.Content);
                return File(stream, "application/octet-stream", ((Candidate)user).CV.Title);
            }
            else
            {
                return NotFound($"Unable to download CV.");
            }
        }
    }
}
