using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TeacherPortal.Interfaces;

namespace TeacherPortal.Controllers
{
    public class ChatsController : Controller
    {
        private readonly IStoreRepository _storeRepository;

        public ChatsController(IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository;
        }

        // GET: Chats
        public async Task<ActionResult> Index()
        {
            var chats = await _storeRepository.GetChats();
            return View(chats);
        }

        // GET: Chats/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var chat = await _storeRepository.GetChat(id);
            return View(chat);
        }


        // GET: Chats/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var deleted = await _storeRepository.DeleteChat(id, "chatroulette");
            return RedirectToAction(nameof(Index));
        }
    }
}