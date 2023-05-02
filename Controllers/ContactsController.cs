using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CrucibleContactPro.Data;
using CrucibleContactPro.Models;
using Microsoft.AspNetCore.Authorization;
using CrucibleContactPro.Enums;
using Microsoft.AspNetCore.Identity;
using CrucibleContactPro.Services.Interfaces;
using CrucibleContactPro.Services;

namespace CrucibleContactPro.Controllers
{
    [Authorize]
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;

        // Dependency Injection
        public ContactsController(ApplicationDbContext context, UserManager<AppUser> userManager, IImageService imageService)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;
        }

        // GET: Contacts
        public async Task<IActionResult> Index()
        {
            string? appUserId = _userManager.GetUserId(User);

            List<Contact> contacts = new List<Contact>();
            contacts = await _context.Contacts.Where(c => c.AppUserId == appUserId).Include(c => c.AppUser).ToListAsync();

            return View(contacts);
        }

        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contacts/Create
        public async Task<IActionResult> Create()
        {
            string? appUserId = _userManager.GetUserId(User);

            IEnumerable<Category> categories = await _context.Categories.Where(c => c.AppUserId == appUserId).ToListAsync();


            ViewData["CategoryList"] = new MultiSelectList(categories, "Id", "CategoryName");

            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());

            Contact contact = new Contact();

            return View(contact);
        }

        // POST: Contacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,DateOfBirth,Address1,Address2,City,State,ZipCode,EmailAddress,PhoneNumber,ImageFile")] Contact contact, IEnumerable<int> selected)
        {

            ModelState.Remove("AppUserId");

            if (ModelState.IsValid)
            {
                contact.AppUserId = _userManager.GetUserId(User);

                contact.CreatedDate = DateTime.UtcNow;

                if (contact.ImageFile != null)
                {
                    contact.ImageBytes = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                    contact.ImageType = contact.ImageFile.ContentType;
                }

                if (contact.DateOfBirth != null)
                {
                    contact.DateOfBirth = DateTime.SpecifyKind(contact.DateOfBirth.Value, DateTimeKind.Utc);
                }


                _context.Add(contact);
                await _context.SaveChangesAsync();

                // Add categories to the Contact
                // TODO: Add as a service call

                foreach (int categoryId in selected)
                {
                    Category? category = await _context.Categories.FindAsync(categoryId);

                    if (contact != null && category != null)
                    {
                        contact.Categories.Add(category);
                    }
                }
                
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(contact);
        }

        // GET: Contacts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            // First Check
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            Contact? contact = await _context.Contacts.FindAsync(id);

            // Second Check
            if (contact == null)
            {
                return NotFound();
            }
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id", contact.AppUserId);
            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CreatedDate,AppUserId,FirstName,LastName,DateOfBirth,Address1,Address2,City,State,ZipCode,EmailAddress,PhoneNumber,ImageBytes,ImageType,ImageFile")] Contact contact)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    contact.CreatedDate = DateTime.SpecifyKind(contact.CreatedDate, DateTimeKind.Utc);

                    if (contact.DateOfBirth != null)
                    {
                        contact.DateOfBirth = DateTime.SpecifyKind(contact.DateOfBirth.Value, DateTimeKind.Utc);
                    }

                    if (contact.ImageFile != null)
                    {
                        contact.ImageBytes = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                        contact.ImageType = contact.ImageFile.ContentType;
                    }

                    _context.Update(contact);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AppUserId"] = new SelectList(_context.Users, "Id", "Id", contact.AppUserId);
            return View(contact);
        }

        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Contacts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Contacts'  is null.");
            }
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
            return (_context.Contacts?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
