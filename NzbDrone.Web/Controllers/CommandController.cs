﻿using System.Web.Mvc;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.Providers;
using NzbDrone.Core.Providers.DownloadClients;
using NzbDrone.Web.Filters;
using NzbDrone.Web.Models;
using System;

namespace NzbDrone.Web.Controllers
{
    public class CommandController : Controller
    {
        private readonly JobProvider _jobProvider;
        private readonly SabProvider _sabProvider;
        private readonly SmtpProvider _smtpProvider;
        private readonly TwitterProvider _twitterProvider;
        private readonly EpisodeProvider _episodeProvider;
        private readonly GrowlProvider _growlProvider;
        private readonly SeasonProvider _seasonProvider;
        private readonly ProwlProvider _prowlProvider;

        public CommandController(JobProvider jobProvider, SabProvider sabProvider,
                                    SmtpProvider smtpProvider, TwitterProvider twitterProvider,
                                    EpisodeProvider episodeProvider, GrowlProvider growlProvider,
                                    SeasonProvider seasonProvider, ProwlProvider prowlProvider)
        {
            _jobProvider = jobProvider;
            _sabProvider = sabProvider;
            _smtpProvider = smtpProvider;
            _twitterProvider = twitterProvider;
            _episodeProvider = episodeProvider;
            _growlProvider = growlProvider;
            _seasonProvider = seasonProvider;
            _prowlProvider = prowlProvider;
        }

        public JsonResult RssSync()
        {
            _jobProvider.QueueJob(typeof(RssSyncJob));
            return JsonNotificationResult.Queued("RSS sync");
        }

        public JsonResult BacklogSearch()
        {
            _jobProvider.QueueJob(typeof(BacklogSearchJob));
            return JsonNotificationResult.Queued("Backlog search");
        }

        public JsonResult RecentBacklogSearch()
        {
            _jobProvider.QueueJob(typeof(RecentBacklogSearchJob));
            return JsonNotificationResult.Queued("Recent backlog search");
        }

        public JsonResult PastWeekBacklogSearch()
        {
            _jobProvider.QueueJob(typeof(PastWeekBacklogSearchJob));
            return JsonNotificationResult.Queued("Past Week backlog search");
        }

        public JsonResult ForceRefresh(int seriesId)
        {
            _jobProvider.QueueJob(typeof(UpdateInfoJob), seriesId);
            _jobProvider.QueueJob(typeof(DiskScanJob), seriesId);
            _jobProvider.QueueJob(typeof(RefreshEpisodeMetadata), seriesId);

            return JsonNotificationResult.Queued("Episode update/Disk scan");
        }

        public JsonResult ForceRefreshAll()
        {
            _jobProvider.QueueJob(typeof(UpdateInfoJob));
            _jobProvider.QueueJob(typeof(DiskScanJob));
            _jobProvider.QueueJob(typeof(RefreshEpisodeMetadata));

            return JsonNotificationResult.Queued("Episode update/Disk scan");
        }

        [HttpPost]
        [JsonErrorFilter]
        public JsonResult GetSabnzbdCategories(string host, int port, string apiKey, string username, string password)
        {
            return new JsonResult { Data = _sabProvider.GetCategories(host, port, apiKey, username, password) };
        }

        [HttpPost]
        public JsonResult TestEmail(string server, int port, bool ssl, string username, string password, string fromAddress, string toAddresses)
        {
            if (_smtpProvider.SendTestEmail(server, port, ssl, username, password, fromAddress, toAddresses))
                return JsonNotificationResult.Info("Successful", "Test email sent.");

            return JsonNotificationResult.Oops("Couldn't send Email, please check your settings");
        }

        public JsonResult GetTwitterAuthorization()
        {
            var result = _twitterProvider.GetAuthorization();

            if (result == null)
                JsonNotificationResult.Oops("Couldn't get Twitter Authorization");

            return new JsonResult { Data = result, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        public JsonResult VerifyTwitterAuthorization(string token, string verifier)
        {
            var result = _twitterProvider.GetAndSaveAccessToken(token, verifier);

            if (!result)
                JsonNotificationResult.Oops("Couldn't verify Twitter Authorization");

            return JsonNotificationResult.Info("Good News!", "Successfully verified Twitter Authorization.");

        }

        public JsonResult RegisterGrowl(string host, string password)
        {
            try
            {
                var split = host.Split(':');
                var hostname = split[0];
                var port = Convert.ToInt32(split[1]);

                _growlProvider.Register(hostname, port, password);
                _growlProvider.TestNotification(hostname, port, password);

                return JsonNotificationResult.Info("Good News!", "Registered and tested growl successfully");
            }
            catch(Exception ex)
            {
                return JsonNotificationResult.Oops("Couldn't register and test Growl");
            }
        }

        [HttpPost]
        public EmptyResult SaveSeasonIgnore(int seriesId, int seasonNumber, bool ignored)
        {
            _seasonProvider.SetIgnore(seriesId, seasonNumber, ignored);
            return new EmptyResult();
        }

        [HttpPost]
        public EmptyResult SaveEpisodeIgnore(int episodeId, bool ignored)
        {
            _episodeProvider.SetEpisodeIgnore(episodeId, ignored);
            return new EmptyResult();
        }

        public JsonResult TestProwl(string apiKeys)
        {
            _prowlProvider.TestNotification(apiKeys);
            return JsonNotificationResult.Info("Good News!", "Test message has been sent to Prowl");
        }

        public JsonResult TestSabnzbd(string host, int port, string apiKey, string username, string password)
        {
            //_prowlProvider.TestNotification(apiKeys);
            var version = _sabProvider.Test(host, port, apiKey, username, password);

            if (String.IsNullOrWhiteSpace(version))
                return JsonNotificationResult.Oops("Failed to connect to SABnzbd, please check your settings");

            return JsonNotificationResult.Info("Success!", "SABnzbd settings have been verified successfully! Version: " + version);
        }
    }
}
