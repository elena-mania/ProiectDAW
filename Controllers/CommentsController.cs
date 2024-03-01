using ProiectDAW.Data;
using ProiectDAW.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace ProiectDAW.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public CommentsController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        //comentariul poate fi sters doar de user (propriile comentarii) sau admin (toate)
        [HttpPost]
        [Authorize(Roles = ("User,Admin"))]
        public IActionResult Delete(int id)
        {
            Comment comm = db.Comments.Find(id);

            if (comm.UserId == _userManager.GetUserId(User)||User.IsInRole("Admin"))
            {
                db.Comments.Remove(comm);
                db.SaveChanges();
                return Redirect("/Topics/Show/" + comm.TopicId);
            }
            else
            {
                TempData["message"] = "Nu aveți dreptul sa stergeti un comentariu care nu vă aparține!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index","Topics");
            }
              
        }
        [Authorize(Roles = ("User,Admin"))]
        public IActionResult Edit(int id)
        {
            Comment comm = db.Comments.Find(id);
            if(comm.UserId == _userManager.GetUserId(User))
            {
                return View(comm);
            }
            else
            {
                TempData["message"] = "Nu aveți dreptul de a edita un comentariu care nu vă aparține!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Topics");
            }
        }

        [HttpPost]
        [Authorize(Roles = ("User,Admin"))]
        public IActionResult Edit(int id, Comment requestComment)
        {
            Comment comm = db.Comments.Find(id);
            if (comm.UserId == _userManager.GetUserId(User))
            {
                if (ModelState.IsValid)
                {

                    comm.Content = requestComment.Content;

                    db.SaveChanges();

                    return Redirect("/Topics/Show/" + comm.TopicId);
                }
                else
                {
                    return View(requestComment);
                }
            }
            else
            {
                TempData["message"] = "Nu aveți dreptul de a edita un comentariu care nu vă aparține!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Topics");
            }

        }
    }
}
