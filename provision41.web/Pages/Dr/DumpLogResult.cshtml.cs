using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using provision41.web.Models;
using provision41.web.Data;
using Microsoft.EntityFrameworkCore;

namespace Provision41Web.Pages.Dr
{
    public class DumpLogResultModel : PageModel
    {
        private readonly AppDbContext _context;

        public DumpLogResultModel(AppDbContext context)
        {
            _context = context;
        }

        public DumpLog DumpLog { get; set; } = new();
        public Truck Truck { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
            {
                DumpLog? log = await _context.DumpLogs.FirstOrDefaultAsync(d => d.Id == id);

                if (log == null)
                {
                    return NotFound();
                }

                DumpLog = log;

                Truck? truck = await _context.Trucks.FirstOrDefaultAsync(t => t.Id == log.TruckId);
                if (truck != null)
                {
                    Truck = truck;
                }

                return Page();
            }
        // public IActionResult OnGet(int id)
        // {
        //     DumpLog? log = _context.DumpLogs.FirstOrDefault(d => d.Id == id);
        //     if (log == null) return NotFound();

        //     DumpLog = log;
        //     return Page();
        // }
    }
}
