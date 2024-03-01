using ProiectDAW.Data;
using ProiectDAW.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProiectDAW.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }
        public IActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.message =
                TempData["message"].ToString();
                ViewBag.Alert = TempData["messageType"];
            }

            var users = from user in db.Users
                        orderby user.UserName
                        select user;
            SetAccess();
            ViewBag.UsersList = users;
            return View();
        }
        //metoda ce verifica conditiile pentru afisarea butoanelor
        private void SetAccess()
        {
            ViewBag.ShowButton = false;
            if (User.IsInRole("User"))
            {
                ViewBag.ShowButton = true;
            }
            ViewBag.IsAdmin = User.IsInRole("Admin");
            ViewBag.CurrentUser = _userManager.GetUserId(User);
        }
        public async Task<ActionResult> Show(string id)
        {
            ApplicationUser user = db.Users.Find(id);
            var roles = await _userManager.GetRolesAsync(user);
            SetAccess();
            ViewBag.Roles = roles;

            return View(user);
        }
        [Authorize(Roles = "Admin,User")]
        public async Task<ActionResult> Edit(string id)
        {
            ApplicationUser user = db.Users.Find(id);

            user.AllRoles = GetAllRoles();

            var roleNames = await _userManager.GetRolesAsync(user); // Lista de nume de roluri

            // Cautam ID-ul rolului in baza de date
            var currentUserRole = _roleManager.Roles
                                              .Where(r => roleNames.Contains(r.Name))
                                              .Select(r => r.Id)
                                              .First(); // Selectam 1 singur rol
            ViewBag.UserRole = currentUserRole;
            
            return View(user);
        }
        [Authorize(Roles = "Admin,User")]
        [HttpPost]
        public async Task<ActionResult> Edit(string id, ApplicationUser newData, [FromForm] string newRole)
        {
            ApplicationUser user = db.Users.Find(id);

            user.AllRoles = GetAllRoles();


            if (ModelState.IsValid)
            {
                if(user.Id == _userManager.GetUserId(User) || User.IsInRole("Admin"))
                {
                    user.UserName = newData.UserName;
                    user.Email = newData.Email;
                    user.FirstName = newData.FirstName;
                    user.LastName = newData.LastName;
                    user.PhoneNumber = newData.PhoneNumber;

                    if (User.IsInRole("Admin"))
                    {
                        // Cautam toate rolurile din baza de date
                        var roles = db.Roles.ToList();

                        foreach (var role in roles)
                        {
                            // Scoatem userul din rolurile anterioare
                            await _userManager.RemoveFromRoleAsync(user, role.Name);
                        }
                        // Adaugam noul rol selectat
                        var roleName = await _roleManager.FindByIdAsync(newRole);
                        await _userManager.AddToRoleAsync(user, roleName.ToString());
                    }
                    

                    db.SaveChanges();
                    TempData["message"] = "Profilul a fost editat cu succes!";
                    TempData["messageType"] = "alert-success";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "Nu aveți dreptul sa editati profilul acestui utilizator!";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }
            }
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost]
        public IActionResult Delete(string id)
        {
            var user = db.Users
                         .Include("Topics")
                         .Include("Comments")
                         .Where(u => u.Id == id)
                         .First();
            if (user.Id == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                if (user.Topics.Count > 0)
                {
                    foreach (var rev in user.Topics)
                    {
                        db.Topics.Remove(rev);
                    }
                }
                if (user.Comments.Count > 0)
                {
                    foreach (var prod in user.Comments)
                    {
                        db.Comments.Remove(prod);
                    }
                }

                db.ApplicationUsers.Remove(user);

                db.SaveChanges();
                TempData["message"] = "Utilizatorul a fost eliminat!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"] = "Nu aveți dreptul să ștergeți acest utilizator!";
                TempData["messageType"] = "alert-danger";
                return Redirect("/Users/Index");
            }
        }

        [NonAction]
        public IEnumerable<SelectListItem> GetAllRoles()
        {
            var selectList = new List<SelectListItem>();

            var roles = from role in db.Roles
                        select role;

            foreach (var role in roles)
            {
                selectList.Add(new SelectListItem
                {
                    Value = role.Id.ToString(),
                    Text = role.Name.ToString()
                });
            }
            return selectList;
        }


    }
}