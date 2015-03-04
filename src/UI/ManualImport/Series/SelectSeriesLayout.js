var _ = require('underscore');
var Marionette = require('marionette');
var SeriesCollection = require('../../Series/SeriesCollection');
var SelectSeriesView = require('./SelectSeriesCollectionView');

module.exports = Marionette.Layout.extend({
    template  : 'ManualImport/Series/SelectSeriesLayoutTemplate',

    regions : {
        series : '.x-series'
    },

    ui : {
        filter : '.x-filter'
    },

    initialize : function() {
        this.seriesCollection = SeriesCollection.clone();
    },

    onRender : function() {
        this.seriesView = new SelectSeriesView({
            selectable : true,
            collection : this.seriesCollection
        });

        this.listenToOnce(this.seriesView, 'selectionChanged', function () {
            this.trigger('selectionChanged');
        });

        this.series.show(this.seriesView);
        this._setupFilter();
    },

    getSelectedSeries : function () {
        return this.seriesView.getSelectedModel();
    },

    _setupFilter : function () {
        var self = this;

        //TODO: This should be a mixin (same as Add Series searching)
        this.ui.filter.keyup(function(e) {
            if (_.contains([
                    9,
                    16,
                    17,
                    18,
                    19,
                    20,
                    33,
                    34,
                    35,
                    36,
                    37,
                    38,
                    39,
                    40,
                    91,
                    92,
                    93
                ], e.keyCode)) {
                return;
            }

            self._filter(self.ui.filter.val());
        });
    },

    _filter : function (term) {
        this.seriesCollection.setFilter(['title', term, 'contains']);
    }
});