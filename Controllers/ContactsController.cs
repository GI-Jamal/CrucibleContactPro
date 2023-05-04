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
        private readonly IAddressBookService _addressBookService;

        // Dependency Injection
        public ContactsController(ApplicationDbContext context, UserManager<AppUser> userManager, IImageService imageService, IAddressBookService addressBookService)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;
            _addressBookService = addressBookService;
        }

        // GET: Contacts
        public async Task<IActionResult> Index()
        {
            string? appUserId = _userManager.GetUserId(User);

            List<Contact> contacts = new List<Contact>();
            contacts = await _context.Contacts.Where(c => c.AppUserId == appUserId).Include(c => c.AppUser).Include(c => c.Categories).ToListAsync();

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
            ViewData["CategoryList"] = await GetCategoriesListAsync();

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
                await _addressBookService.AddCategoriesToContactAsync(selected, contact.Id);
                
                return RedirectToAction(nameof(Index));
            }
           
            ViewData["CategoryList"] = await GetCategoriesListAsync(selected);
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());

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

            Contact? contact = await _context.Contacts.Include(c => c.Categories).FirstOrDefaultAsync(c => c.Id == id);

            // Second Check
            if (contact == null)
            {
                return NotFound();
            }

            ViewData["CategoryList"] = await GetCategoriesListAsync(contact.Categories.Select(c => c.Id));
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());

            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CreatedDate,AppUserId,FirstName,LastName,DateOfBirth,Address1,Address2,City,State,ZipCode,EmailAddress,PhoneNumber,ImageBytes,ImageType,ImageFile")] Contact contact, IEnumerable<int> selected)
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

                    // Handle Categories
                    if (selected != null)
                    {
                        // Remove current categories
                        await _addressBookService.RemoveCategoriesFromContactAsync(contact.Id);

                        // Add the updated categories
                        await _addressBookService.AddCategoriesToContactAsync(selected, contact.Id);
                    }

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
           
            ViewData["CategoryList"] = await GetCategoriesListAsync(selected);
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>());
            
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

        private async Task<MultiSelectList> GetCategoriesListAsync(IEnumerable<int> categoryIds = null!)
        {
            string? appUserId = _userManager.GetUserId(User);

            IEnumerable<Category> categories = await _context.Categories.Where(c => c.AppUserId == appUserId).ToListAsync();

            return new MultiSelectList(categories, "Id", "CategoryName", categoryIds);
        }
    }
}
