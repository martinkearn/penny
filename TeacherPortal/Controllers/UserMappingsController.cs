using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using TeacherPortal.Interfaces;

namespace TeacherPortal.Controllers
{
    public class UserMappingsController : Controller
    {
        
        private readonly IStoreRepository _storeRepository;

        public UserMappingsController(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
        }


        // GET: UserMappings
        public async Task<ActionResult> Index()
        {
            var usermappings = await _storeRepository.GetUserMappings();
            return View(usermappings);
        }

      
        // GET: UserMappings/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UserMappings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here
                UserMapping _usermapping = new UserMapping();
                _usermapping.User1 = collection["User1"];
                _usermapping.User2 = collection["User2"];
                
                _storeRepository.InsertUserMapping(_usermapping, "usermappings");
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UserMappings/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: UserMappings/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id, IFormCollection collection)
        {
            try
            {
                _storeRepository.DeleteUserMapping("usermappings", id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}