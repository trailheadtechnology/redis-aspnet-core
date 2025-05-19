using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using redis_aspnet_core_example.Data;
using redis_aspnet_core_example.Helpers;
using SkiaSharp;

namespace redis_aspnet_core_example.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IDistributedCache _cache;
        private readonly HybridCache _hybridCache;
        
        public IndexModel(IDistributedCache cache, HybridCache hybridCache)
        {
            _cache = cache;
            _hybridCache = hybridCache;
        }

        public List<User> Users { get; set; } = new List<User>();

        public void OnGet()
        {
            using (var ctx = new MyDbContext())
            {
                this.Users = ctx.Users.ToList();
            }
        }

        // No cache version
        public async Task<IActionResult> OnGetFilePreview(int id)
        {
            using (var ctx = new MyDbContext())
            {
                var bytes = ctx.Users.FirstOrDefault(u => u.UserId == id)?.FullProfilePicture ?? new byte[] { };
                return File(bytes, "image/octet-stream", "profile.jpg");
            }
        }
        
        //  // StackExchange.Redis helper version
        //  public async Task<IActionResult> OnGetFilePreview(int id)
        //  {
        //      var key = $"user:{id}:photo";
        //
        //      var bytes = await StackExchangeRedisHelper.GetAsync<byte[]>(key);
        //      if (bytes == null)
        //      {
        //          // get the profile photo
        //          using (var ctx = new MyDbContext())
        //          {
        //              bytes = ctx.Users.FirstOrDefault(u => u.UserId == id)?.FullProfilePicture ?? new byte[] { };
        //          }
        //
        //          // generate thumbnail
        //          bytes = ThumbnailHelper.CreateSquareThumbnail(bytes, maxDimension: 100);
        //
        //          // cache the profile photo
        //          await StackExchangeRedisHelper.SetAsync(key, bytes, expiryMinutes: 120);
        //      }
        //     return File(bytes, "image/octet-stream", "profile.jpg");
        // }
        
        // // IDistributedCache version
        // public async Task<IActionResult> OnGetFilePreview(int id)
        // {
        //     var key = $"user:{id}:photo";
        //
        //     var bytes = await _cache.GetAsync(key);
        //     if (bytes == null)
        //     {
        //         // get the profile photo
        //         using (var ctx = new MyDbContext())
        //         {
        //             bytes = ctx.Users.FirstOrDefault(u => u.UserId == id)?.FullProfilePicture ?? new byte[] { };
        //         }
        //
        //         // generate thumbnail
        //         bytes = ThumbnailHelper.CreateSquareThumbnail(bytes, maxDimension: 100);
        //
        //         // cache the profile photo
        //         await _cache.SetAsync(key, bytes);
        //     }
        //
        //     return File(bytes, "image/octet-stream", "profile.jpg");
        // }
        
        // // HybridCache version
        // public async Task<IActionResult> OnGetFilePreview(int id)
        // {
        //     string cacheKey = $"user:{id}:photo";
        //     // Try to get the thumbnail from the HybridCache (L1 in-memory, L2 distributed)
        //     byte[] thumbnail = await _hybridCache.GetOrCreateAsync(cacheKey, async cancellationToken =>
        //     {
        //         using (var ctx = new MyDbContext())
        //         {
        //             var user = await ctx.Users.FindAsync(new object[] { id }, cancellationToken);
        //             if (user?.FullProfilePicture == null) return []; 
        //             
        //             byte[] thumbBytes = ThumbnailHelper.CreateSquareThumbnail(user.FullProfilePicture);
        //             return thumbBytes;   
        //         }
        //     });
        //
        //     if (thumbnail == null) return NotFound(); 
        //
        //     return File(thumbnail, "image/octet-stream", "profile.jpg");
        // }
    }
}