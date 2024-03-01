using ProiectDAW.Data;
using ProiectDAW.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Humanizer;
using System.Text.RegularExpressions;

namespace ProiectDAW.Controllers
{
    public class TopicsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public TopicsController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        //Pentru fiecare topic se afiseaza userul care l a initiat

        public IActionResult Index(string sortBy, string sortOrder)
        {
            var topics = db.Topics.Include("Category").Include("User");
            var searchTerm = "";

            // MOTOR DE CAUTARE
            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                searchTerm = Convert.ToString(HttpContext.Request.Query["search"]).Trim();
                
                List<int> topicIds = db.Topics
                    .Where(at => at.Title.Contains(searchTerm) || at.Content.Contains(searchTerm))
                    .Select(a => a.Id).ToList();

                
                List<int> topicIdsOfCommentsWithSearchString = db.Comments
                    .Where(c => c.Content.Contains(searchTerm))
                    .Select(c => (int)c.TopicId).ToList();

                List<int> mergedIds = topicIds.Union(topicIdsOfCommentsWithSearchString).ToList();

                topics = db.Topics
                    .Where(topic => mergedIds.Contains(topic.Id))
                    .Include("Category")
                    .Include("User")
                    .OrderBy(a => a.Date);
            }

            switch (sortBy)
            {
                case "Title":
                    topics = (sortOrder == "desc") ? topics.OrderByDescending(t => t.Title) : topics.OrderBy(t => t.Title);
                    break;
                case "Category":
                    topics = (sortOrder == "desc") ? topics.OrderByDescending(t => t.Category.CategoryName) : topics.OrderBy(t => t.Category.CategoryName);
                    break;
                default:
                    topics = (sortOrder == "desc") ? topics.OrderByDescending(t => t.Date) : topics.OrderBy(t => t.Date);
                    break;
            }


            ViewBag.SearchString = searchTerm;
            if (TempData.ContainsKey("message"))
            {
                ViewBag.message =
                TempData["message"].ToString();
                ViewBag.Alert = TempData["messageType"];
            }

            int totalItems = topics.Count();
            var currentPage =
            Convert.ToInt32(HttpContext.Request.Query["page"]);
            int _perPage = 3;

            var offset = 0;

            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }

            var paginatedTopics = topics.Skip(offset).Take(_perPage);


            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);

            ViewBag.Topics = paginatedTopics;
            if (searchTerm != "")
            {
                ViewBag.PaginationBaseUrl = "/Topics/Index/?search=" + searchTerm + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Topics/Index/?page";
            }

            return View();
        }


        //la fiecare topic apare autorul
        [HttpGet]
        public IActionResult Show(int id, string sortOrder, string sortBy)
        {
            Topic topic = db.Topics.Include("Category").Include("Comments").Include("User").Include("Comments.User")
                                       .Where(top => top.Id == id)
                                       .First();

            SetAccess();

            var comments = db.Comments.Where(c => c.TopicId == id);

            ViewBag.SortOrder = string.IsNullOrEmpty(sortOrder) ? "desc" : sortOrder;
            ViewBag.SortBy = string.IsNullOrEmpty(sortBy) ? "Date" : sortBy;

            switch (sortBy)
            {
                case "Content":
                    comments = (sortOrder == "desc") ? comments.OrderByDescending(c => c.Content) : comments.OrderBy(c => c.Content);
                    break;
                default:
                    comments = (sortOrder == "desc") ? comments.OrderByDescending(c => c.Date) : comments.OrderBy(c => c.Date);
                    break;
            }

            ViewBag.Comments = comments.ToList();

            return View(topic);
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

        [HttpPost]
        [Authorize(Roles ="User,Admin")]
        public IActionResult Show([FromForm] Comment comment)
        {
            comment.Date = DateTime.Now;
            comment.UserId=_userManager.GetUserId(User);

            if(ModelState.IsValid)
            {
                db.Comments.Add(comment);
                db.SaveChanges();
                return Redirect("/Topics/Show/" + comment.TopicId);
            }
            else
            {
                Topic top = db.Topics.Include("Category").Include("User").Include("Comments").
                                       Include("Comments.User").Where(top => top.Id == comment.TopicId).First();
                SetAccess();
                return View(top);
            }
        }

        //doar utilizatorul cu rol de user poate adauga un topic nou
        [Authorize(Roles="User,Admin")]
        public IActionResult New()
        {
            Topic topic = new Topic();

            topic.Categ = GetAllCategories();

            return View(topic);
        }
        [Authorize(Roles = "User,Admin")]
        [HttpPost]
        public IActionResult New(Topic topic)
        {
            topic.Date = DateTime.Now;

            //preluam utilizatorul care posteaza
            topic.UserId = _userManager.GetUserId(User);
            topic.Categ = GetAllCategories();

            if (ModelState.IsValid)
            {
                db.Topics.Add(topic);
                db.SaveChanges();
                TempData["message"] = "Subiectul de discuție a fost adăugat";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                topic.Categ = GetAllCategories();
                return View(topic);
            }
        }
        //doar utilizatorii inregistrati pot edita topicurile (asta include si adminii care le-au adaugat)
        
        [HttpGet]
        [Authorize(Roles="User,Admin")]
        public IActionResult Edit(int id)
        {
            Topic topic = db.Topics.Include("Category")
                                         .Where(top => top.Id == id)
                                         .First();

            topic.Categ = GetAllCategories();

            if (topic.UserId==_userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(topic); //fiecare utilizator editeaza doar propriile topicuri
            }
            else
            {
                TempData["message"] = "Nu aveți dreptul de a edita un subiect care nu vă aparține!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");

            }
        }
        //verificam rolul utilizatorilor care au dreptul sa editeze
        [HttpPost]
        [Authorize(Roles ="User,Admin")]
        public IActionResult Edit(int id, Topic requestTopic)
        {
            Topic topic = db.Topics.Find(id);
           

            if (ModelState.IsValid)
            {
                if (topic.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
                {
                    topic.Title = requestTopic.Title;
                    topic.Content = requestTopic.Content;
                    topic.CategoryId = requestTopic.CategoryId;
                    TempData["message"] = "Subiectul de discuție a fost modificat";
                    TempData["messageType"] = "alert-success";
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "Nu aveți dreptul de a edita un subiect care nu vă aparține!";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                requestTopic.Categ = GetAllCategories();
                return View(requestTopic);
            }
        }
        //Subiectele pot fi sterse de catre autorul postarii sau de cartre Admin
        [HttpPost]
        [Authorize(Roles =("User,Admin"))]
        public ActionResult Delete(int id)
        {
            Topic topic = db.Topics.Include("Comments")
                                    .Where(top=>top.Id==id)
                                    .First();

            if(topic.UserId==_userManager.GetUserId(User)||User.IsInRole("Admin"))
            {
                db.Comments.RemoveRange(topic.Comments);
                db.Topics.Remove(topic);
                db.SaveChanges();
                TempData["message"] = "Subiectul de discuție a fost eliminat!";
                TempData["messageType"] = "alert-success"; 
                return RedirectToAction("Index");

            }
            else
            {
                TempData["message"] = "Nu aveți dreptul să ștergeți un subiect care nu vă aparține!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }

        [NonAction]

        public IEnumerable<SelectListItem> GetAllCategories()
        {
            var selectList = new List<SelectListItem>();

            var categories = from cat in db.Categories
                             select cat;

            foreach (var category in categories)
            {
                selectList.Add(new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = category.CategoryName
                });
            }
            return selectList;
        }
        public IActionResult IndexNou()
        {
            return View();
        }
    }
}




