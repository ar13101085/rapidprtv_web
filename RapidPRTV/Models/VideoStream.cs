using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using Quartz;
using RapidPRTV.Hubs;

namespace RapidPRTV.Video
{
    public class VideoStream
    {
        private readonly string _filename;

        public VideoStream(string filename)
        {
            _filename = filename ;
        }

        //public static int length = 0;

        public static PushStreamContent LiveContent;
        public static HttpResponseMessage response;
        public static int length = 0;
        public static List<VideoCell> VideoCells = new List<VideoCell>();


        public void WriteToStream()
        {
            try
            {
                VideoCells.Clear();
               

                using (var video = File.Open(_filename, FileMode.Open, FileAccess.Read))
                {

                    var length = (int) video.Length;
                    var bytesRead = 1;
                    while (length > 0 && bytesRead > 0)
                    {
                        byte[] buffer = new byte[65536];
                        bytesRead = video.Read(buffer, 0, Math.Min(length, buffer.Length));
                        VideoCells.Add(new VideoCell()
                        {
                            Bytes = buffer,
                            Len = bytesRead
                        });
                        length -= bytesRead;
                    }
                }
            }
            catch (HttpException ex)
            {
                return;
            }
            finally
            {
                TvHub.VideoLive("api/Video/Live");
            }
        }
        

        /*public async void WriteToStream(Stream outputStream, HttpContent content, TransportContext context)
        {
            try
            {
                byte[] buffer = new byte[65536];

                using (var video = File.Open(_filename, FileMode.Open, FileAccess.Read))
                {
                    var length = (int)video.Length;
                    /*if(length==0)
                        length = (int)video.Length;#1#
                    var bytesRead = 1;

                    while (length > 0 && bytesRead > 0)
                    {
                        bytesRead = video.Read(buffer, 0, Math.Min(length, buffer.Length));
                        VideoCells.Add(new VideoCell()
                        {
                            Bytes = buffer,
                            Len = bytesRead
                        });
                        //await outputStream.WriteAsync(buffer, 0, bytesRead);
                        length -= bytesRead;
                        
                        
                    }
                }
            }
            catch (HttpException ex)
            {
                return;
            }
            finally
            {
                outputStream.Close();
                
            }
        }*/

        
    }

    public class VideoCell
    {
        public byte[] Bytes { get; set; }
        public int Len { get; set; }

    }
}