using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Manual
{
    public class ManualImportService : IExecute<ManualImportCommand>
    {
        private readonly ISeriesService _seriesService;
        private readonly IEpisodeService _episodeService;
        private readonly IVideoFileInfoReader _videoFileInfoReader;
        private readonly IImportApprovedEpisodes _importApprovedEpisodes;
        private readonly Logger _logger;

        public ManualImportService(ISeriesService seriesService,
                                   IEpisodeService episodeService,
                                   IVideoFileInfoReader videoFileInfoReader,
                                   IImportApprovedEpisodes importApprovedEpisodes,
                                   Logger logger)
        {
            _seriesService = seriesService;
            _episodeService = episodeService;
            _videoFileInfoReader = videoFileInfoReader;
            _importApprovedEpisodes = importApprovedEpisodes;
            _logger = logger;
        }

        public void Execute(ManualImportCommand message)
        {
            var series = _seriesService.GetSeries(message.SeriesId);
            var episodes = _episodeService.GetEpisodes(message.EpisodeIds);
            var parsedEpisodeInfo = Parser.Parser.ParsePath(message.Path) ?? new ParsedEpisodeInfo();
            var quality = new QualityModel(Quality.FindById(message.QualityId));
            var mediaInfo = _videoFileInfoReader.GetMediaInfo(message.Path);
            var existingFile = series.Path.IsParentPath(message.Path);

            var localEpisode = new LocalEpisode
                               {
                                   ExistingFile = false,
                                   Episodes = episodes,
                                   MediaInfo = mediaInfo,
                                   ParsedEpisodeInfo = parsedEpisodeInfo,
                                   Path = message.Path,
                                   Quality = quality,
                                   Series = series,
                                   Size = 0
                               };

            var importDecision = new ImportDecision(localEpisode);

            //TODO: Can we get the downloadClient item? (Lookup by path)?
            
            _importApprovedEpisodes.Import(new List<ImportDecision> {importDecision}, !existingFile, null);
        }
    }
}
