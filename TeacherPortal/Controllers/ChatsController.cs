using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TeacherPortal.Interfaces;
using TeacherPortal.Models;

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
        public async Task<ActionResult> Details(string id, string onlyShowUser, bool hideParticipantNames, string newestMessageTimestamp)
        {
            var chat = await _storeRepository.GetChat(id);

            var messageTimes = chat.Messages.Select(o => o.Time).ToList();

            //filter on message time if defined
            var newestMessageTimestampAsDate = Convert.ToDateTime(newestMessageTimestamp);
            if (!string.IsNullOrEmpty(newestMessageTimestamp))
            {
                chat.Messages = chat.Messages.Where(o => o.Time <= newestMessageTimestampAsDate).ToList();
            }

            //filter on user if defined
            if (!string.IsNullOrEmpty(onlyShowUser))
            {
                chat.Messages = chat.Messages
                    .Where(o => o.UserId.ToLower() == onlyShowUser.ToLower())
                    .ToList();
            }

            //if hideParticipantNames, replace names with user numbers
            if (hideParticipantNames)
            {
                for (int i = 1; i <= chat.Participants.Count; i++)
                {
                    var originalName = chat.Participants[i - 1];
                    var newName = $"User {i.ToString()}";
                    foreach (var message in chat.Messages.Where(o => o.UserId == originalName))
                    {
                        message.UserId = newName;
                    }
                }
            }


            var vm = new ChatDetailsViewModel()
            {
                Chat = chat,
                MessageTimes = messageTimes,
                HideParticipantNames = hideParticipantNames,
                OnlyShowUser = onlyShowUser,
                NewestMessageTimestamp = newestMessageTimestampAsDate
            };

            return View(vm);
        }


        // GET: Chats/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var deleted = await _storeRepository.DeleteChat(id, "penny");
            return RedirectToAction(nameof(Index));
        }

        // GET: Chats/Delete
        public async Task<ActionResult> DeleteAll()
        {
            var deleted = await _storeRepository.DeleteAllChats("penny");
            return RedirectToAction(nameof(Index));
        }
    }
}