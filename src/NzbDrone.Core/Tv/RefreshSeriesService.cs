﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.DataAugmentation.DailySeries;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Tv.Commands;
using NzbDrone.Core.Tv.Events;

namespace NzbDrone.Core.Tv
{
    public class RefreshSeriesService : IExecute<RefreshSeriesCommand>
    {
        private readonly IProvideSeriesInfo _seriesInfo;
        private readonly ISeriesService _seriesService;
        private readonly IRefreshEpisodeService _refreshEpisodeService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDailySeriesService _dailySeriesService;
        private readonly IDiskScanService _diskScanService;
        private readonly ICheckIfSeriesShouldBeRefreshed _checkIfSeriesShouldBeRefreshed;
        private readonly Logger _logger;

        public RefreshSeriesService(IProvideSeriesInfo seriesInfo,
                                    ISeriesService seriesService,
                                    IRefreshEpisodeService refreshEpisodeService,
                                    IEventAggregator eventAggregator,
                                    IDailySeriesService dailySeriesService,
                                    IDiskScanService diskScanService,
                                    ICheckIfSeriesShouldBeRefreshed checkIfSeriesShouldBeRefreshed,
                                    Logger logger)
        {
            _seriesInfo = seriesInfo;
            _seriesService = seriesService;
            _refreshEpisodeService = refreshEpisodeService;
            _eventAggregator = eventAggregator;
            _dailySeriesService = dailySeriesService;
            _diskScanService = diskScanService;
            _checkIfSeriesShouldBeRefreshed = checkIfSeriesShouldBeRefreshed;
            _logger = logger;
        }

        private void RefreshSeriesInfo(Series series)
        {
            _logger.ProgressInfo("Updating Info for {0}", series.Title);
            var tuple = _seriesInfo.GetSeriesInfo(series.TvdbId);

            var seriesInfo = tuple.Item1;

            series.Title = seriesInfo.Title;
            series.TitleSlug = seriesInfo.TitleSlug;
            series.TvRageId = seriesInfo.TvRageId;
            series.ImdbId = seriesInfo.ImdbId;
            series.AirTime = seriesInfo.AirTime;
            series.Overview = seriesInfo.Overview;
            series.Status = seriesInfo.Status;
            series.CleanTitle = seriesInfo.CleanTitle;
            series.SortTitle = seriesInfo.SortTitle;
            series.LastInfoSync = DateTime.UtcNow;
            series.Runtime = seriesInfo.Runtime;
            series.Images = seriesInfo.Images;
            series.Network = seriesInfo.Network;
            series.FirstAired = seriesInfo.FirstAired;
            series.Ratings = seriesInfo.Ratings;
            series.Actors = seriesInfo.Actors;
            series.Genres = seriesInfo.Genres;
            series.Certification = seriesInfo.Certification;

            if (_dailySeriesService.IsDailySeries(series.TvdbId))
            {
                series.SeriesType = SeriesTypes.Daily;
            }

            try
            {
                series.Path = new DirectoryInfo(series.Path).FullName;
                series.Path = series.Path.GetActualCasing();
            }
            catch (Exception e)
            {
                _logger.WarnException("Couldn't update series path for " + series.Path, e);
            }

            series.Seasons = UpdateSeasons(series, seriesInfo);

            _seriesService.UpdateSeries(series);
            _refreshEpisodeService.RefreshEpisodeInfo(series, tuple.Item2);

            _logger.Debug("Finished series refresh for {0}", series.Title);
            _eventAggregator.PublishEvent(new SeriesUpdatedEvent(series));
        }

        private List<Season> UpdateSeasons(Series series, Series seriesInfo)
        {
            foreach (var season in seriesInfo.Seasons)
            {
                var existingSeason = series.Seasons.SingleOrDefault(s => s.SeasonNumber == season.SeasonNumber);

                //Todo: Should this should use the previous season's monitored state?
                if (existingSeason == null)
                {
                    if (season.SeasonNumber == 0)
                    {
                        season.Monitored = false;
                        continue;
                    }

                    _logger.Debug("New season ({0}) for series: [{1}] {2}, setting monitored to true", season.SeasonNumber, series.TvdbId, series.Title);
                    season.Monitored = true;
                }

                else
                {
                    season.Monitored = existingSeason.Monitored;
                }
            }

            return seriesInfo.Seasons;
        }

        public void Execute(RefreshSeriesCommand message)
        {
            _eventAggregator.PublishEvent(new SeriesRefreshStartingEvent());

            if (message.SeriesId.HasValue)
            {
                var series = _seriesService.GetSeries(message.SeriesId.Value);
                RefreshSeriesInfo(series);
            }
            else
            {
                var allSeries = _seriesService.GetAllSeries().OrderBy(c => c.Title).ToList();

                foreach (var series in allSeries)
                {
                    if (message.Trigger == CommandTrigger.Manual || _checkIfSeriesShouldBeRefreshed.ShouldRefresh(series))
                    {
                        try
                        {
                            RefreshSeriesInfo(series);
                        }
                        catch (Exception e)
                        {
                            _logger.ErrorException("Couldn't refresh info for {0}".Inject(series), e);
                        }
                    }

                    else
                    {
                        try
                        {
                            _logger.Info("Skipping refresh of series: {0}", series.Title);
                            _diskScanService.Scan(series);
                        }
                        catch (Exception e)
                        {
                            _logger.ErrorException("Couldn't rescan series {0}".Inject(series), e);
                        }
                    }
                }
            }
        }
    }
}
