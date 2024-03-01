using ProiectDAW.Data;
using ProiectDAW.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace ProiectDAW.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public CategoriesController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public ActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"].ToString();
            }

            var categories = from category in db.Categories
                             orderby category.CategoryName
                             select category;
            ViewBag.Categories = categories;
            return View();
        }
     
        public ActionResult Show(int id)
        {
            int _perPage = 5; 
            int currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);

            Category category = db.Categories.Include(c => c.Topics).FirstOrDefault(c => c.Id == id);

            var totalItems = category.Topics.Count();
            var offset = 0;

            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }

            var topicsPaginate = category.Topics.OrderBy(t => t.Date).Skip(offset).Take(_perPage);

            ViewBag.LastPage = Math.Ceiling((float)totalItems / (float)_perPage);
            ViewBag.Topics = topicsPaginate;
            ViewBag.CurrentPage = currentPage;

            return View(category);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult New(Category cat)
        {
            if (ModelState.IsValid)
            {
                db.Categories.Add(cat);
                db.SaveChanges();
                TempData["message"] = "Categoria a fost adaugata";
                return RedirectToAction("Index");
            }
            else
            {
                return View(cat);
            }
        }
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id)
        {
            Category category = db.Categories.Find(id);
            return View(category);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id, Category requestCategory)
        {
            Category category = db.Categories.Find(id);

            if (ModelState.IsValid)
            {
                category.CategoryName = requestCategory.CategoryName;
                db.SaveChanges();
                TempData["message"] = "Categoria a fost modificata!";
                return RedirectToAction("Index");

            }
            else
            {
                return View(requestCategory);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            Category category = db.Categories.Include("Topics").Include("Topics.Comments")
                                              .Where(cat => cat.Id == id).FirstOrDefault();

            if (category != null)
            {
                foreach (var topic in category.Topics) ///pentru fiecare topic din categorie
                {
                    db.Comments.RemoveRange(topic.Comments);///sterge comentariile asociate
                }

                db.Topics.RemoveRange(category.Topics);//dupa care sterge topicurile
                db.Categories.Remove(category);
                db.SaveChanges();
                TempData["message"] = "Categoria a fost stearsa";
            }
            else
            {
                TempData["message"] = "Categoria nu a fost gasita";
            }

            return RedirectToAction("Index");
        }
    }
}

