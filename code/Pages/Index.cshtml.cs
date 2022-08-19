using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using redis_aspnet_core_example.Data;
using redis_aspnet_core_example.Helpers;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Encoder = System.Drawing.Imaging.Encoder;

namespace redis_aspnet_core_example.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IDistributedCache _cache;

        public IndexModel(IDistributedCache cache)
        {
            _cache = cache;
        }

        public List<User> Users { get; set; } = new List<User>();

        public void OnGet()
        {
            using (var ctx = new MyDbContext())
            {
                this.Users = ctx.Users.ToList();
            }
        }

        // no cache
        public async Task<IActionResult> OnGetFilePreview(int id)
        {
            using (var ctx = new MyDbContext())
            {
                var bytes = ctx.Users.FirstOrDefault(u => u.UserId == id)?.FullProfilePicture ?? new byte[] { };
                return File(bytes, "image/octet-stream", "profile.jpg");
            }
        }

        //// redis version
        //public async Task<IActionResult> OnGetFilePreview(int id)
        //{
        //    var key = $"user:{id}:photo";

        //    var bytes = await CacheHelper.GetAsync<byte[]>(key);
        //    if (bytes == null)
        //    {
        //        // get the profile photo
        //        using (var ctx = new MyDbContext())
        //        {
        //            bytes = ctx.Users.FirstOrDefault(u => u.UserId == id)?.FullProfilePicture ?? new byte[] { };
        //        }

        //        // generate thumbnail
        //        bytes = CreateSquareThumbnail(bytes, maxDimension: 100);

        //        // cache the profile photo
        //        await CacheHelper.SetAsync(key, bytes, expiryMinutes: 120);
        //    }

        //    return File(bytes, "image/octet-stream", "profile.jpg");
        //}

        //// IDistributedCache
        //public async Task<IActionResult> OnGetFilePreview(int id)
        //{
        //    var key = $"user:{id}:photo";

        //    var bytes = await _cache.GetAsync(key);
        //    if (bytes == null)
        //    {
        //        // get the profile photo
        //        using (var ctx = new MyDbContext())
        //        {
        //            bytes = ctx.Users.FirstOrDefault(u => u.UserId == id)?.FullProfilePicture ?? new byte[] { };
        //        }

        //        // generate thumbnail
        //        bytes = CreateSquareThumbnail(bytes, maxDimension: 100);

        //        // cache the profile photo
        //        await _cache.SetAsync(key, bytes);
        //    }

        //    return File(bytes, "image/octet-stream", "profile.jpg");
    }

        private static byte[] CreateSquareThumbnail(byte[] myBytes, int maxDimension = 280)
        {
            Bitmap sourceImage;
            using (var ms = new MemoryStream(myBytes))
            {
                sourceImage = new Bitmap(ms);
            }

            //Obtain source dimensions and initialize scaled dimensions and crop offsets
            int sourceWidth = sourceImage.Width;
            int sourceHeight = sourceImage.Height;
            int scaledSourceWidth = 0;
            int scaledSourceHeight = 0;
            int offsetWidth = 0;
            int offsetHeight = 0;

            //Calculate cropping offset
            if (sourceWidth >= sourceHeight)
            {
                offsetWidth = (sourceWidth - sourceHeight) / 2;
                scaledSourceWidth = (int)Math.Round(sourceWidth / ((double)sourceHeight / maxDimension));
                scaledSourceHeight = maxDimension;
            }
            else if (sourceHeight > sourceWidth)
            {
                offsetHeight = (sourceHeight - sourceWidth) / 2;
                scaledSourceHeight = (int)Math.Round(sourceHeight / ((double)sourceWidth / maxDimension));
                scaledSourceWidth = maxDimension;
            }

            //Create new thumbnail image of height and width defined in thumbSize
            Bitmap thumbnail = new Bitmap(maxDimension, maxDimension, PixelFormat.Format24bppRgb);
            thumbnail.SetResolution(sourceImage.HorizontalResolution, sourceImage.VerticalResolution);

            using (var graphics = Graphics.FromImage(thumbnail))
            {
                //Draw source image scaled down with aspect ratio maintained onto the thumbnail with the offset
                graphics.CompositingQuality = CompositingQuality.HighSpeed;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.DrawImage(sourceImage,
                    new Rectangle(0, 0, scaledSourceWidth, scaledSourceHeight),
                    offsetWidth, offsetHeight, sourceWidth, sourceHeight, GraphicsUnit.Pixel);

                //Push thumbnail onto stream for upload
                using (MemoryStream stream = new MemoryStream())
                {
                    var encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(Encoder.Compression, Convert.ToByte("90"));
                    var codecInfo = ImageCodecInfo.GetImageDecoders().FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);
                    thumbnail.Save(stream, codecInfo, encoderParameters);
                    stream.Position = 0;

                    return stream.ToArray();
                }
            }
        }

    }
}