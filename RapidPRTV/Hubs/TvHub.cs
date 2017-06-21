using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using Common.Logging.Simple;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Quartz;
using Quartz.Impl;
using RapidPRTV.Models;

namespace RapidPRTV.Hubs
{
    public class TvHub : Hub
    {
        private static readonly IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<TvHub>();

        public void Hello()
        {
            Clients.All.hello("Hello Arif");
        }

        public static void HelloTest()
        {
            hubContext.Clients.All.hello("Hello Arif Jahan");
        }

        public static void SetText(string s)
        {
            hubContext.Clients.All.Text(s);
        }

        public static void ViewAdvertise(string s)
        {
            hubContext.Clients.All.AdvertiseView(s);
        }

        public static void VideoLive(string s)
        {
            hubContext.Clients.All.VideoLive(s);
        }

        public static void VideoLiveStop()
        {
            hubContext.Clients.All.VideoLiveStop("stop");
        }

        public static void LengthCheck(string len)
        {
            hubContext.Clients.All.len(len);
        }

        public static void Test(string len)
        {
            hubContext.Clients.All.test(len);
        }

        public string GetAdvertiseData(Advertise advertise)
        {
            var s = JsonConvert.SerializeObject(new
            {
                id = advertise.AdvertiseId,
                address = advertise.GetPath(),
                duration = advertise.LiveDurationInSec,
                boxName = advertise.BoxName
            });
            return s;
        }
    }

    public class VideoPlay : IJob
    {
        public static int POSITION_VIDEO;
        public ApplicationDbContext Db = new ApplicationDbContext();

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                var allPublishVideo =
                        Db.Videos.Where(x => x.NowPublish)
                            .OrderBy(y => y.VideoUploadTime)
                            .ToList();
                if (!allPublishVideo.Any())
                {
                    return;
                }

                if (POSITION_VIDEO >= allPublishVideo.Count())
                {
                    POSITION_VIDEO = 0;
                }

                var res = Path.GetExtension(allPublishVideo[POSITION_VIDEO].VideoName);

                allPublishVideo[POSITION_VIDEO].VideoPublishTime = DateTime.Now;
                Db.SaveChanges();

                TvHub.VideoLive(JsonConvert.SerializeObject(new
                {
                    address = allPublishVideo[POSITION_VIDEO].VideoLiveLink,
                    seek_position = 0
                }));

                Debug.WriteLine(JsonConvert.SerializeObject(new
                {
                    address = allPublishVideo[POSITION_VIDEO].VideoLiveLink,
                    seek_position = 0
                }));

                context.Scheduler.PauseTrigger(context.Trigger.Key);
                Thread.Sleep((Convert.ToInt32(allPublishVideo[POSITION_VIDEO].VideoDuration) * 1000) + (2 * 1000));
                context.Scheduler.ResumeTrigger(context.Trigger.Key);
                POSITION_VIDEO++;
            }
            catch (Exception e)
            {
                
                Debug.WriteLine(" VideoPlay   "+e.ToString());
            }
        }

        public static void VideoPlayListControl()
        {
            var schedulerVideoPlay = StdSchedulerFactory.GetDefaultScheduler();
            var job1 = JobBuilder.Create<VideoPlay>()
                .UsingJobData("Video", "v")
                .Build();
            var trigger1 = TriggerBuilder.Create()
                .WithIdentity("video", "group19")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(1)
                    .RepeatForever()
                )
                .Build();
            schedulerVideoPlay.ScheduleJob(job1, trigger1);
        }
    }


    public class LiveControl : IJob
    {
        public ApplicationDbContext Db = new ApplicationDbContext();

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                var allPublishVideo =
                        Db.Videos.Where(x => x.NowPublish)
                            .OrderBy(y => y.VideoUploadTime)
                            .ToList();
                if (!allPublishVideo.Any())
                {
                    return;
                }


                var res = Path.GetExtension(allPublishVideo[VideoPlay.POSITION_VIDEO].VideoName);
                /*var uploadFilesDir = HttpContext.Current.Server.MapPath("~/Video");
                if (!Directory.Exists(uploadFilesDir))
                {
                    Directory.CreateDirectory(uploadFilesDir);
                }*/
                var millisecondPlay =
                    (int)(DateTime.Now - allPublishVideo[VideoPlay.POSITION_VIDEO].VideoPublishTime).TotalMilliseconds;

                TvHub.VideoLive(JsonConvert.SerializeObject(new
                {
                    address = allPublishVideo[VideoPlay.POSITION_VIDEO].VideoLiveLink,
                    seek_position = millisecondPlay
                }));

                Debug.WriteLine(JsonConvert.SerializeObject(new
                {
                    address = allPublishVideo[VideoPlay.POSITION_VIDEO].VideoLiveLink,
                    seek_position = millisecondPlay
                }));

                context.Scheduler.PauseTrigger(context.Trigger.Key);
                Thread.Sleep(5 * 1000);
                context.Scheduler.ResumeTrigger(context.Trigger.Key);
            }
            catch (Exception e)
            {

                Debug.WriteLine("LiveControl  "+e.ToString());
            }
        }

        public static void ControlLive()
        {
            var schedulerVideoPlay = StdSchedulerFactory.GetDefaultScheduler();
            var job1 = JobBuilder.Create<LiveControl>()
                .UsingJobData("live", "video")
                .Build();
            var trigger1 = TriggerBuilder.Create()
                .WithIdentity("video", "group7")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(1)
                    .RepeatForever()
                )
                .Build();
            schedulerVideoPlay.ScheduleJob(job1, trigger1);
        }
    }


    public class TextTaskSchedule : IJob
    {
        public static int POSITION_TEXT;
        public static int TIME_PREVIEW_TEXT = 5;
        public ApplicationDbContext Db = new ApplicationDbContext();

        public void Execute(IJobExecutionContext context)
        {
            var allText = Db.Texts.Where(x => x.IsPublishNow).OrderBy(y => y.TextPublishTime).ToList();

            if (POSITION_TEXT >= allText.Count())
            {
                POSITION_TEXT = 0;
            }
            TvHub.SetText(allText[POSITION_TEXT].TextContent);
            POSITION_TEXT++;

            /*var trigger1 = TriggerBuilder.Create()
               .WithIdentity("trigger1", "group1")
               .StartNow()
               .WithSimpleSchedule(s => s
                   .WithIntervalInSeconds(GetTime())
                   .RepeatForever()
               )
               .Build();

            context.Scheduler.RescheduleJob(context.Trigger.Key, trigger1);*/

            //Thread.Sleep(TIME_PREVIEW_TEXT*1000);


            /*var scheduler = StdSchedulerFactory.GetDefaultScheduler();
            scheduler.Start();
            var job = JobBuilder.Create<TextTaskSchedule>().Build();
            var trigger = TriggerBuilder.Create()
                //.WithIdentity("trigger10", "group2")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(/*TIME_PREVIEW_TEXT#1#GetTime())
                    //.RepeatForever()
                    .WithRepeatCount(0)
                    )
                

                .Build();
            scheduler.ScheduleJob(job, trigger);*/
            //context.Scheduler.Standby();
            //Thread.Sleep(10000);
            //Thread.Sleep(TimeSpan.FromSeconds(10));
            //context.Scheduler.Start();

            /*var job = JobBuilder.Create<TextTaskSchedule>().Build();
            var trigger = TriggerBuilder.Create()
                .WithIdentity("trigger10", "group2")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(GetTime())
                    .RepeatForever()
                    )
                //.WithSchedule()


                .Build();

            context.Scheduler.ScheduleJob(job, trigger);
            context.Scheduler.Start();*/
            /*context.Scheduler.Standby();
            context.Scheduler.StartDelayed(TimeSpan.FromSeconds(10));ss*/
        }

        public static void Start()
        {
            /*var scheduler = StdSchedulerFactory.GetDefaultScheduler();
            scheduler.Start();
            var job = JobBuilder.Create<TextTaskSchedule>().Build();
            var trigger = TriggerBuilder.Create()
                .WithIdentity("trigger10", "group2")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(/*TIME_PREVIEW_TEXT#1#GetTime())
                    .RepeatForever()
                    )
                
                .Build();
            scheduler.ScheduleJob(job, trigger);*/
            var scheduler = StdSchedulerFactory.GetDefaultScheduler();
            scheduler.Start();
            var job = JobBuilder.Create<TextTaskSchedule>().Build();
            var trigger = TriggerBuilder.Create()
                .WithIdentity("trigger10", "group2")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds( /*TIME_PREVIEW_TEXT*/5)
                    .RepeatForever()
                )
                //.WithSchedule()
                .Build();
            scheduler.ScheduleJob(job, trigger);
        }

        public static int GetTime()
        {
            var rnd = new Random();
            var month = rnd.Next(5, 10);
            return month;
        }
    }


    public class Ad1 : IJob
    {
        public static int POSITION_ADVERTISE;
        public static bool isView;
        public ApplicationDbContext Db = new ApplicationDbContext();

        public void Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;

            var boxName = dataMap.GetString("BOX_NAME");
            var allAdvertise =
                Db.Advertises.Where(x => x.IsPublish && x.BoxName == boxName)
                    .OrderBy(y => y.AdvertisePublishTime)
                    .ToList();
            if (!allAdvertise.Any())
            {
                if (isView) TvHub.ViewAdvertise(GetAdvertiseData(new Advertise {BoxName = boxName}));
                isView = false;

                return;
            }

            if (POSITION_ADVERTISE >= allAdvertise.Count())
            {
                POSITION_ADVERTISE = 0;
            }
            Debug.WriteLine(GetAdvertiseData(allAdvertise[POSITION_ADVERTISE]));
            TvHub.ViewAdvertise(GetAdvertiseData(allAdvertise[POSITION_ADVERTISE]));
            context.Scheduler.PauseTrigger(context.Trigger.Key);
            Thread.Sleep(allAdvertise[POSITION_ADVERTISE].LiveDurationInSec*1000);
            context.Scheduler.ResumeTrigger(context.Trigger.Key);
            POSITION_ADVERTISE++;
        }

        public string GetAdvertiseData(Advertise advertise)
        {
            var s = JsonConvert.SerializeObject(new
            {
                id = advertise.AdvertiseId,
                address = advertise.GetPath(),
                duration = advertise.LiveDurationInSec,
                boxName = advertise.BoxName
            });
            return s;
        }
    }

    public class Ad2 : IJob
    {
        public static int POSITION_ADVERTISE;
        public static bool isView;
        public ApplicationDbContext Db = new ApplicationDbContext();

        public void Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;

            var boxName = dataMap.GetString("BOX_NAME");
            var allAdvertise =
                Db.Advertises.Where(x => x.IsPublish && x.BoxName == boxName)
                    .OrderBy(y => y.AdvertisePublishTime)
                    .ToList();
            if (!allAdvertise.Any())
            {
                if (isView) TvHub.ViewAdvertise(GetAdvertiseData(new Advertise {BoxName = boxName}));
                isView = false;

                return;
            }

            if (POSITION_ADVERTISE >= allAdvertise.Count())
            {
                POSITION_ADVERTISE = 0;
            }
            TvHub.ViewAdvertise(GetAdvertiseData(allAdvertise[POSITION_ADVERTISE]));
            context.Scheduler.PauseTrigger(context.Trigger.Key);
            Thread.Sleep(allAdvertise[POSITION_ADVERTISE].LiveDurationInSec*1000);
            context.Scheduler.ResumeTrigger(context.Trigger.Key);
            POSITION_ADVERTISE++;
        }

        public string GetAdvertiseData(Advertise advertise)
        {
            var s = JsonConvert.SerializeObject(new
            {
                id = advertise.AdvertiseId,
                address = advertise.GetPath(),
                duration = advertise.LiveDurationInSec,
                boxName = advertise.BoxName
            });
            return s;
        }
    }

    public class Ad3 : IJob
    {
        public static int POSITION_ADVERTISE;
        public static bool isView;
        public ApplicationDbContext Db = new ApplicationDbContext();

        public void Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;

            var boxName = dataMap.GetString("BOX_NAME");
            var allAdvertise =
                Db.Advertises.Where(x => x.IsPublish && x.BoxName == boxName)
                    .OrderBy(y => y.AdvertisePublishTime)
                    .ToList();
            if (!allAdvertise.Any())
            {
                if (isView) TvHub.ViewAdvertise(GetAdvertiseData(new Advertise {BoxName = boxName}));
                isView = false;

                return;
            }

            if (POSITION_ADVERTISE >= allAdvertise.Count())
            {
                POSITION_ADVERTISE = 0;
            }
            TvHub.ViewAdvertise(GetAdvertiseData(allAdvertise[POSITION_ADVERTISE]));

            context.Scheduler.PauseTrigger(context.Trigger.Key);
            Thread.Sleep(allAdvertise[POSITION_ADVERTISE].LiveDurationInSec*1000);
            context.Scheduler.ResumeTrigger(context.Trigger.Key);
            POSITION_ADVERTISE++;
        }

        public string GetAdvertiseData(Advertise advertise)
        {
            var s = JsonConvert.SerializeObject(new
            {
                id = advertise.AdvertiseId,
                address = advertise.GetPath(),
                duration = advertise.LiveDurationInSec,
                boxName = advertise.BoxName
            });
            return s;
        }
    }

    public class Ad4 : IJob
    {
        public static int POSITION_ADVERTISE;
        public static bool isView;
        public ApplicationDbContext Db = new ApplicationDbContext();

        public void Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;

            var boxName = dataMap.GetString("BOX_NAME");
            var allAdvertise =
                Db.Advertises.Where(x => x.IsPublish && x.BoxName == boxName)
                    .OrderBy(y => y.AdvertisePublishTime)
                    .ToList();
            if (!allAdvertise.Any())
            {
                if (isView) TvHub.ViewAdvertise(GetAdvertiseData(new Advertise {BoxName = boxName}));
                isView = false;

                return;
            }

            if (POSITION_ADVERTISE >= allAdvertise.Count())
            {
                POSITION_ADVERTISE = 0;
            }
            TvHub.ViewAdvertise(GetAdvertiseData(allAdvertise[POSITION_ADVERTISE]));

            context.Scheduler.PauseTrigger(context.Trigger.Key);
            Thread.Sleep(allAdvertise[POSITION_ADVERTISE].LiveDurationInSec*1000);
            context.Scheduler.ResumeTrigger(context.Trigger.Key);
            POSITION_ADVERTISE++;
        }

        public string GetAdvertiseData(Advertise advertise)
        {
            var s = JsonConvert.SerializeObject(new
            {
                id = advertise.AdvertiseId,
                address = advertise.GetPath(),
                duration = advertise.LiveDurationInSec,
                boxName = advertise.BoxName
            });
            return s;
        }
    }

    public class Ad5 : IJob
    {
        public static int POSITION_ADVERTISE;
        public static bool isView;
        public ApplicationDbContext Db = new ApplicationDbContext();

        public void Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;

            var boxName = dataMap.GetString("BOX_NAME");
            var allAdvertise =
                Db.Advertises.Where(x => x.IsPublish && x.BoxName == boxName)
                    .OrderBy(y => y.AdvertisePublishTime)
                    .ToList();
            if (!allAdvertise.Any())
            {
                if (isView) TvHub.ViewAdvertise(GetAdvertiseData(new Advertise {BoxName = boxName}));
                isView = false;

                return;
            }

            if (POSITION_ADVERTISE >= allAdvertise.Count())
            {
                POSITION_ADVERTISE = 0;
            }
            TvHub.ViewAdvertise(GetAdvertiseData(allAdvertise[POSITION_ADVERTISE]));

            context.Scheduler.PauseTrigger(context.Trigger.Key);
            Thread.Sleep(allAdvertise[POSITION_ADVERTISE].LiveDurationInSec*1000);
            context.Scheduler.ResumeTrigger(context.Trigger.Key);
            POSITION_ADVERTISE++;
        }

        public string GetAdvertiseData(Advertise advertise)
        {
            var s = JsonConvert.SerializeObject(new
            {
                id = advertise.AdvertiseId,
                address = advertise.GetPath(),
                duration = advertise.LiveDurationInSec,
                boxName = advertise.BoxName
            });
            return s;
        }
    }

    public class Ad6 : IJob
    {
        public static int POSITION_ADVERTISE;
        public static bool isView;
        public ApplicationDbContext Db = new ApplicationDbContext();

        public void Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;

            var boxName = dataMap.GetString("BOX_NAME");
            var allAdvertise =
                Db.Advertises.Where(x => x.IsPublish && x.BoxName == boxName)
                    .OrderBy(y => y.AdvertisePublishTime)
                    .ToList();
            if (!allAdvertise.Any())
            {
                if (isView) TvHub.ViewAdvertise(GetAdvertiseData(new Advertise {BoxName = boxName}));
                isView = false;

                return;
            }

            if (POSITION_ADVERTISE >= allAdvertise.Count())
            {
                POSITION_ADVERTISE = 0;
            }
            TvHub.ViewAdvertise(GetAdvertiseData(allAdvertise[POSITION_ADVERTISE]));

            context.Scheduler.PauseTrigger(context.Trigger.Key);
            Thread.Sleep(allAdvertise[POSITION_ADVERTISE].LiveDurationInSec*1000);
            context.Scheduler.ResumeTrigger(context.Trigger.Key);
            POSITION_ADVERTISE++;
        }

        public string GetAdvertiseData(Advertise advertise)
        {
            var s = JsonConvert.SerializeObject(new
            {
                id = advertise.AdvertiseId,
                address = advertise.GetPath(),
                duration = advertise.LiveDurationInSec,
                boxName = advertise.BoxName
            });
            return s;
        }
    }

    public class Ad7 : IJob
    {
        public static int POSITION_ADVERTISE;
        public static bool isView;
        public ApplicationDbContext Db = new ApplicationDbContext();

        public void Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;

            var boxName = dataMap.GetString("BOX_NAME");
            var allAdvertise =
                Db.Advertises.Where(x => x.IsPublish && x.BoxName == boxName)
                    .OrderBy(y => y.AdvertisePublishTime)
                    .ToList();
            if (!allAdvertise.Any())
            {
                if (isView) TvHub.ViewAdvertise(GetAdvertiseData(new Advertise {BoxName = boxName}));
                isView = false;

                return;
            }

            if (POSITION_ADVERTISE >= allAdvertise.Count())
            {
                POSITION_ADVERTISE = 0;
            }
            TvHub.ViewAdvertise(GetAdvertiseData(allAdvertise[POSITION_ADVERTISE]));

            context.Scheduler.PauseTrigger(context.Trigger.Key);
            Thread.Sleep(allAdvertise[POSITION_ADVERTISE].LiveDurationInSec*1000);
            context.Scheduler.ResumeTrigger(context.Trigger.Key);
            POSITION_ADVERTISE++;
        }

        public string GetAdvertiseData(Advertise advertise)
        {
            var s = JsonConvert.SerializeObject(new
            {
                id = advertise.AdvertiseId,
                address = advertise.GetPath(),
                duration = advertise.LiveDurationInSec,
                boxName = advertise.BoxName
            });
            return s;
        }
    }

    public class Ad8 : IJob
    {
        public static int POSITION_ADVERTISE;
        public static bool isView;
        public ApplicationDbContext Db = new ApplicationDbContext();

        public void Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;

            var boxName = dataMap.GetString("BOX_NAME");
            var allAdvertise =
                Db.Advertises.Where(x => x.IsPublish && x.BoxName == boxName)
                    .OrderBy(y => y.AdvertisePublishTime)
                    .ToList();
            if (!allAdvertise.Any())
            {
                if (isView) TvHub.ViewAdvertise(GetAdvertiseData(new Advertise {BoxName = boxName}));
                isView = false;

                return;
            }

            if (POSITION_ADVERTISE >= allAdvertise.Count())
            {
                POSITION_ADVERTISE = 0;
            }
            TvHub.ViewAdvertise(GetAdvertiseData(allAdvertise[POSITION_ADVERTISE]));

            context.Scheduler.PauseTrigger(context.Trigger.Key);
            Thread.Sleep(allAdvertise[POSITION_ADVERTISE].LiveDurationInSec*1000);
            context.Scheduler.ResumeTrigger(context.Trigger.Key);
            POSITION_ADVERTISE++;
        }

        public string GetAdvertiseData(Advertise advertise)
        {
            var s = JsonConvert.SerializeObject(new
            {
                id = advertise.AdvertiseId,
                address = advertise.GetPath(),
                duration = advertise.LiveDurationInSec,
                boxName = advertise.BoxName
            });
            return s;
        }
    }

    public class Ad9 : IJob
    {
        public static int POSITION_ADVERTISE;
        public static bool isView;
        public ApplicationDbContext Db = new ApplicationDbContext();

        public void Execute(IJobExecutionContext context)
        {
            var dataMap = context.JobDetail.JobDataMap;

            var boxName = dataMap.GetString("BOX_NAME");
            var allAdvertise =
                Db.Advertises.Where(x => x.IsPublish && x.BoxName == boxName)
                    .OrderBy(y => y.AdvertisePublishTime)
                    .ToList();
            if (!allAdvertise.Any())
            {
                if (isView) TvHub.ViewAdvertise(GetAdvertiseData(new Advertise {BoxName = boxName}));
                isView = false;

                return;
            }

            if (POSITION_ADVERTISE >= allAdvertise.Count())
            {
                POSITION_ADVERTISE = 0;
            }
            TvHub.ViewAdvertise(GetAdvertiseData(allAdvertise[POSITION_ADVERTISE]));

            context.Scheduler.PauseTrigger(context.Trigger.Key);
            Thread.Sleep(allAdvertise[POSITION_ADVERTISE].LiveDurationInSec*1000);
            context.Scheduler.ResumeTrigger(context.Trigger.Key);
            POSITION_ADVERTISE++;
        }

        public string GetAdvertiseData(Advertise advertise)
        {
            var s = JsonConvert.SerializeObject(new
            {
                id = advertise.AdvertiseId,
                address = advertise.GetPath(),
                duration = advertise.LiveDurationInSec,
                boxName = advertise.BoxName
            });
            return s;
        }
    }


    public class AdvertiseTaskSchedule
    {
        public static void Start()
        {
            var scheduler1 = StdSchedulerFactory.GetDefaultScheduler();
            var job1 = JobBuilder.Create<Ad1>()
                .UsingJobData("BOX_NAME", "L-1")
                .Build();
            var trigger1 = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(1)
                    .RepeatForever()
                )
                .Build();
            scheduler1.ScheduleJob(job1, trigger1);


            var scheduler2 = StdSchedulerFactory.GetDefaultScheduler();
            var job2 = JobBuilder.Create<Ad2>()
                .UsingJobData("BOX_NAME", "L-2")
                .Build();
            var trigger2 = TriggerBuilder.Create()
                .WithIdentity("trigger2", "group1")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(1)
                    .RepeatForever()
                )
                .Build();
            scheduler2.ScheduleJob(job2, trigger2);

            var scheduler3 = StdSchedulerFactory.GetDefaultScheduler();
            var job3 = JobBuilder.Create<Ad3>()
                .UsingJobData("BOX_NAME", "L-3")
                .Build();
            var trigger3 = TriggerBuilder.Create()
                .WithIdentity("trigger3", "group1")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(1)
                    .RepeatForever()
                )
                .Build();
            scheduler3.ScheduleJob(job3, trigger3);

            var scheduler4 = StdSchedulerFactory.GetDefaultScheduler();
            var job4 = JobBuilder.Create<Ad4>()
                .UsingJobData("BOX_NAME", "M-1")
                .Build();
            var trigger4 = TriggerBuilder.Create()
                .WithIdentity("trigger4", "group1")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(1)
                    .RepeatForever()
                )
                .Build();
            scheduler4.ScheduleJob(job4, trigger4);

            var scheduler5 = StdSchedulerFactory.GetDefaultScheduler();
            var job5 = JobBuilder.Create<Ad5>()
                .UsingJobData("BOX_NAME", "M-2")
                .Build();
            var trigger5 = TriggerBuilder.Create()
                .WithIdentity("trigger5", "group1")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(1)
                    .RepeatForever()
                )
                .Build();
            scheduler5.ScheduleJob(job5, trigger5);

            var scheduler6 = StdSchedulerFactory.GetDefaultScheduler();
            var job6 = JobBuilder.Create<Ad6>()
                .UsingJobData("BOX_NAME", "M-3")
                .Build();
            var trigger6 = TriggerBuilder.Create()
                .WithIdentity("trigger6", "group1")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(1)
                    .RepeatForever()
                )
                .Build();
            scheduler6.ScheduleJob(job6, trigger6);

            var scheduler7 = StdSchedulerFactory.GetDefaultScheduler();
            var job7 = JobBuilder.Create<Ad7>()
                .UsingJobData("BOX_NAME", "R-1")
                .Build();
            var trigger7 = TriggerBuilder.Create()
                .WithIdentity("trigger7", "group1")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(1)
                    .RepeatForever()
                )
                .Build();
            scheduler7.ScheduleJob(job7, trigger7);

            var scheduler8 = StdSchedulerFactory.GetDefaultScheduler();
            var job8 = JobBuilder.Create<Ad8>()
                .UsingJobData("BOX_NAME", "R-2")
                .Build();
            var trigger8 = TriggerBuilder.Create()
                .WithIdentity("trigger8", "group1")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(1)
                    .RepeatForever()
                )
                .Build();
            scheduler8.ScheduleJob(job8, trigger8);

            var scheduler9 = StdSchedulerFactory.GetDefaultScheduler();
            var job9 = JobBuilder.Create<Ad9>()
                .UsingJobData("BOX_NAME", "R-3")
                .Build();
            var trigger9 = TriggerBuilder.Create()
                .WithIdentity("trigger9", "group1")
                .StartNow()
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(1)
                    .RepeatForever()
                )
                .Build();
            scheduler9.ScheduleJob(job9, trigger9);
        }
    }
}