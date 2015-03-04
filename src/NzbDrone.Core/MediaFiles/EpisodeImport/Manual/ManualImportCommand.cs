using System.Collections.Generic;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.MediaFiles.EpisodeImport.Manual
{
    public class ManualImportCommand : Command
    {
        public string Path { get; set; }
        public int SeriesId { get; set; }
        public List<int> EpisodeIds { get; set; }
        public int QualityId { get; set; }
    }
}
