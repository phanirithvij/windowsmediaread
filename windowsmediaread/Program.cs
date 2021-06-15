using System;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;
using System.IO;
using System.Windows.Media.Imaging;
// https://stackoverflow.com/a/26539524/8608146
using System.Runtime.InteropServices.WindowsRuntime;

// Need to do this for resolving WPF errors
// https://github.com/dotnet/wpf/issues/2341#issuecomment-567723885


// TODO https://stackoverflow.com/q/65161740/8608146 event callbacks
// TODO IMPORTANT move to c++ as self-contained .exe file is too big 130+ MB
// BEFORE ADDING NEW STUFF MOVE TO C++ ASAP

namespace windowsmediaread
{
    public static class Program
    {
        // https://stackoverflow.com/a/63099881/8608146
        public static async Task Main()
        {
            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;
            try
            {
                ostrm = new FileStream("./Redirect.txt", FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open Redirect.txt for writing");
                Console.WriteLine(e.Message);
                return;
            }
            Console.SetOut(writer);
            Console.SetError(writer);

            var gsmtcsm = await GetSystemMediaTransportControlsSessionManager();
            var session = gsmtcsm.GetCurrentSession();
            if (session != null)
            {
                // TODO we can control it
                session.TryTogglePlayPauseAsync().AsTask().RunSynchronously();

                Console.WriteLine(session.GetPlaybackInfo().PlaybackStatus);
                var mediaProperties = await GetMediaProperties(session);

                // TODO save to json or db or something
                Console.WriteLine("{0} - {1} - {2}", mediaProperties.Artist, mediaProperties.Title, mediaProperties.PlaybackType);
                foreach (string genre in mediaProperties.Genres)
                {
                    Console.WriteLine("Genre {0}", genre);
                }
                Console.WriteLine("{0} - {1}", mediaProperties.Subtitle, mediaProperties.AlbumTitle);
                Console.WriteLine("{0} - {1} - {2}", mediaProperties.AlbumArtist, mediaProperties.AlbumTrackCount, mediaProperties.TrackNumber);
                if (mediaProperties.Thumbnail != null)
                {
                    //https://github.com/microsoft/Windows-universal-samples/blob/b1cb20f191d3fd99ce89df50c5b7d1a6e2382c01/archived/PhoneCall/cs/Helpers/ContactItem.cs#L45
                    using IRandomAccessStreamWithContentType thumbnailStream = await mediaProperties.Thumbnail.OpenReadAsync();
                    if (thumbnailStream != null && thumbnailStream.Size > 0)
                    {
                        Byte[] bytes = new Byte[thumbnailStream.Size];
                        // https://stackoverflow.com/q/15328084/8608146
                        await thumbnailStream.ReadAsync(bytes.AsBuffer(), (uint)thumbnailStream.Size, InputStreamOptions.None);
                        var image = ToImage(bytes);
                        Save(image, "saved.png");
                    }
                }
            }
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
        }

        private static async Task<GlobalSystemMediaTransportControlsSessionManager> GetSystemMediaTransportControlsSessionManager() =>
            await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

        private static async Task<GlobalSystemMediaTransportControlsSessionMediaProperties> GetMediaProperties(GlobalSystemMediaTransportControlsSession session) =>
            await session?.TryGetMediaPropertiesAsync();

        public static void Save(this BitmapImage image, string filePath)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using FileStream fileStream = new FileStream(filePath, FileMode.Create);
            encoder.Save(fileStream);
        }

        public static BitmapImage ToImage(byte[] array)
        {
            // https://stackoverflow.com/a/14337202/8608146
            using var ms = new System.IO.MemoryStream(array);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad; // here
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }
    }
}