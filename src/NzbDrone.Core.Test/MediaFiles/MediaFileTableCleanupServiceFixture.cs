﻿using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.MediaFiles
{
    public class MediaFileTableCleanupServiceFixture : CoreTest<MediaFileTableCleanupService>
    {
        private const string DELETED_PATH = "ANY FILE WITH THIS PATH IS CONSIDERED DELETED!";
        private List<Episode> _episodes;
        private Series _series;

        [SetUp]
        public void SetUp()
        {
            _episodes = Builder<Episode>.CreateListOfSize(10)
                  .Build()
                  .ToList();

            _series = Builder<Series>.CreateNew()
                                     .Build();

            Mocker.GetMock<IDiskProvider>()
                  .Setup(e => e.FileExists(It.Is<String>(c => !c.Contains(DELETED_PATH))))
                  .Returns(true);

            Mocker.GetMock<IEpisodeService>()
                  .Setup(c => c.GetEpisodeBySeries(It.IsAny<int>()))
                  .Returns(_episodes);
        }

        private void GivenEpisodeFiles(IEnumerable<EpisodeFile> episodeFiles)
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(c => c.GetFilesBySeries(It.IsAny<int>()))
                  .Returns(episodeFiles.ToList());
        }

        private void GivenFilesAreNotAttachedToEpisode()
        {
            _episodes.ForEach(e => e.EpisodeFileId = 0);

            Mocker.GetMock<IEpisodeService>()
                  .Setup(c => c.GetEpisodeBySeries(It.IsAny<int>()))
                  .Returns(_episodes);
        }

        [Test]
        public void should_skip_files_that_exist_in_disk()
        {
            var episodeFiles = Builder<EpisodeFile>.CreateListOfSize(10)
                .Build();

            GivenEpisodeFiles(episodeFiles);

            Subject.Clean(_series);

            Mocker.GetMock<IEpisodeService>().Verify(c => c.UpdateEpisode(It.IsAny<Episode>()), Times.Never());
        }

        [Test]
        public void should_delete_non_existent_files()
        {
            var episodeFiles = Builder<EpisodeFile>.CreateListOfSize(10)
                .Random(2)
                .With(c => c.RelativePath = DELETED_PATH)
                .Build();

            GivenEpisodeFiles(episodeFiles);

            Subject.Clean(_series);

            Mocker.GetMock<IMediaFileService>().Verify(c => c.Delete(It.Is<EpisodeFile>(e => e.RelativePath == DELETED_PATH), DeleteMediaFileReason.MissingFromDisk), Times.Exactly(2));
        }

        [Test]
        public void should_delete_files_that_dont_belong_to_any_episodes()
        {
            var episodeFiles = Builder<EpisodeFile>.CreateListOfSize(10)
                                .Random(10)
                                .With(c => c.RelativePath = "ExistingPath")
                                .Build();

            GivenEpisodeFiles(episodeFiles);
            GivenFilesAreNotAttachedToEpisode();

            Subject.Clean(_series);

            Mocker.GetMock<IMediaFileService>().Verify(c => c.Delete(It.IsAny<EpisodeFile>(), DeleteMediaFileReason.NoLinkedEpisodes), Times.Exactly(10));
        }

        [Test]
        public void should_unlink_episode_when_episodeFile_does_not_exist()
        {
            GivenEpisodeFiles(new List<EpisodeFile>());

            Subject.Clean(_series);

            Mocker.GetMock<IEpisodeService>().Verify(c => c.UpdateEpisode(It.Is<Episode>(e => e.EpisodeFileId == 0)), Times.Exactly(10));
        }

        [Test]
        public void should_not_update_episode_when_episodeFile_exists()
        {
            var episodeFiles = Builder<EpisodeFile>.CreateListOfSize(10)
                                .Random(10)
                                .With(c => c.RelativePath = "ExistingPath")
                                .Build();

            GivenEpisodeFiles(episodeFiles);

            Subject.Clean(_series);

            Mocker.GetMock<IEpisodeService>().Verify(c => c.UpdateEpisode(It.IsAny<Episode>()), Times.Never());
        }
    }
}
