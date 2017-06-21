using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using System.Web.Routing;
using Microsoft.AspNet.Identity;
using RapidPRTV.Hubs;
using RapidPRTV.Models;
using RapidPRTV.Video;

namespace RapidPRTV.Controllers
{
    [System.Web.Mvc.RoutePrefix("api/video")]
    public class VideoController : ApiController
    {
        public ApplicationDbContext Db = new ApplicationDbContext();

       /* public IHttpActionResult GetVideoInfo()
        {
            TvHub.HelloTest();
            var o = new
            {
                userId = User.Identity.GetUserId()
            };
            return Ok(o);
        }*/

        public HttpResponseMessage GetFileStream(string filename, string ext)
        {
            try
            {
                var video = new VideoStream(filename);

                var response = Request.CreateResponse();

                // response.Content = new PushStreamContent((Action<Stream, HttpContent, TransportContext>) video.WriteToStream, new MediaTypeHeaderValue("video/" + ext));
                response.Content = VideoStream.LiveContent;
                return response;
            }
            catch (Exception e)
            {
                var response = Request.CreateResponse();
                response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(e.ToString(), Encoding.UTF8, "text/plain");
                return response;
            }
        }

        [HttpGet]
        public HttpResponseMessage Live()
        {
            
            var v = Db.Videos.FirstOrDefault(x => x.NowPublish);

            var ext = v.VideoName.Split('.');
            var fullPath = System.Web.Hosting.HostingEnvironment.MapPath("~/Video/" + v.VideoId + "." + ext[1]);


            var video = new VideoStream(fullPath);

            var response = Request.CreateResponse();

            //response.Content = new PushStreamContent((Action<Stream, HttpContent, TransportContext>)video.WriteToStream, new MediaTypeHeaderValue("video/" + ext[1]));
            response.Content = new PushStreamContent(async (stream, httpContent, transportContext) =>
            {
                try
                {
                   
                    foreach (var a in VideoStream.VideoCells)
                    {
                        await stream.WriteAsync(a.Bytes, 0, a.Len);
                    }
                }
                catch (Exception e)
                {
                    return;
                }
                finally
                {
                    stream.Close();
                }
            }, new MediaTypeHeaderValue("video/" + ext[1]));
            return response;
        }
        
        /*[HttpGet]
        public HttpResponseMessage Live()
        {
            //if(VideoStream.response==null)
            HttpResponseMessage response = Request.CreateResponse();
            response.Content = VideoStream.LiveContent;
            return response;
        }*/
        [HttpGet]
        [Route("AllStatus")]
        public IHttpActionResult AllStatus()
        {
            return Ok(new
            {
                /*res=VideoStream.response==null,
                str = VideoStream.LiveContent==null,*/
                VideoStream.length
            });
        }
        [HttpGet]
        public IHttpActionResult ResData()
        {
            return Ok(new
            {
                res=VideoStream.response==null,
                str = VideoStream.LiveContent==null,
                VideoStream.length
            });
        }
    }
}