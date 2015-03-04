var _ = require('underscore');
var Marionette = require('marionette');
var Backgrid = require('backgrid');
var EpisodeCollection = require('../../Series/EpisodeCollection');
var LoadingView = require('../../Shared/LoadingView');
var SelectAllCell = require('../../Cells/SelectAllCell');
var EpisodeNumberCell = require('../../Series/Details/EpisodeNumberCell');
var RelativeDateCell = require('../../Cells/RelativeDateCell');
var SelectEpisodeRow = require('./SelectEpisodeRow');

module.exports = Marionette.Layout.extend({
    template  : 'ManualImport/Episode/SelectEpisodeLayoutTemplate',

    regions : {
        episodes : '.x-episodes'
    },

    columns : [
        {
            name       : '',
            cell       : SelectAllCell,
            headerCell : 'select-all',
            sortable   : false
        },
        {
            name  : 'episodeNumber',
            label : '#',
            cell  : EpisodeNumberCell
        },
        {
            name           : 'title',
            label          : 'Title',
            hideSeriesLink : true,
            cell           : 'string',
            sortable       : false
        },
        {
            name  : 'airDateUtc',
            label : 'Air Date',
            cell  : RelativeDateCell
        }
    ],

    initialize : function(options) {
        this.series = options.series;
        this.season = options.season;
    },

    onRender : function() {
        this.episodes.show(new LoadingView());

        this.episodeCollection = new EpisodeCollection({ seriesId : this.series.id });
        this.episodeCollection.fetch();

        this.listenToOnce(this.episodeCollection, 'sync', function () {

            this.episodeView = new Backgrid.Grid({
                columns    : this.columns,
                collection : this.episodeCollection.bySeason(this.season.get('seasonNumber')),
                className  : 'table table-hover season-grid',
                row        : SelectEpisodeRow,
            });

            this.episodes.show(this.episodeView);
        });
    },

    getSelectedEpisodes : function () {
        return this.episodeView.getSelectedModels();
    }
});
