var _ = require('underscore');
var vent = require('vent');
var Backbone = require('backbone');
var Marionette = require('marionette');
var SeasonCollection = require('../Series/SeasonCollection');
var ProfileSchemaCollection = require('../Settings/Profile/ProfileSchemaCollection');
var BreadcrumbModel = require('./Breadcrumb/BreadcrumbModel');
var BreadcrumbCollection = require('./Breadcrumb/BreadcrumbCollection');
var BreadcrumbView = require('./Breadcrumb/BreadcrumbCollectionView');
var SelectFolderView = require('./Folder/SelectFolderView');
var SelectFileLayout = require('./File/SelectFileLayout');
var SelectSeriesLayout = require('./Series/SelectSeriesLayout');
var SelectSeasonView = require('./Season/SelectSeasonCollectionView');
var SelectEpisodeLayout = require('./Episode/SelectEpisodeLayout');
var SelectQualityView = require('./Quality/SelectQualityCollectionView');
var SummaryView = require('./Summary/ManualImportSummaryView');
var CommandController = require('../Commands/CommandController');
var Messenger = require('../Shared/Messenger');

module.exports = Marionette.Layout.extend({
    className : 'modal-lg',
    template  : 'ManualImport/ManualImportLayoutTemplate',

    regions : {
        breadcrumb : '.x-breadcrumb',
        workspace  : '.x-workspace'
    },

    ui : {
        backButton   : '.x-back',
        nextButton   : '.x-next',
        importButton : '.x-import',
        subtitle     : '.x-subtitle'
    },

    events : {
        'click .x-back'   : '_back',
        'click .x-next'   : '_next',
        'click .x-import' : '_import'
    },

    initialize : function(options) {
        this.folder = options.folder;
        this.file = options.file;

        //TODO: remove (just for testing)
        //this.folder = 'C:\\Test';
        this.file = 'C:\\Test\\Unsorted\\s01e01.mkv';

        this.steps = [
            {
                view     : this._showSelectFolder,
                callback : this._storeFolder
            },
            {
                view     : this._showSelectFile,
                callback : this._storeFile
            },
            {
                view     : this._showSelectSeries,
                callback : this._storeSeries
            },
            {
                view     : this._showSelectSeason,
                callback : this._storeSeason
            },
            {
                view     : this._showSelectEpisodes,
                callback : this._storeEpisodes,
                showNextButton : true
            },
            {
                view     : this._showSelectQuality,
                callback : this._storeQuality
            },
            {
                view     : this._showSummary,
                callback : this._import
            }
        ];
    },

    onRender : function() {
        this.ui.importButton.hide();
        this.ui.backButton.hide();
        this.ui.nextButton.hide();

        this._showBreadcrumbs();

        if (this.folder) {
            this._addBreadcrumb({ step: 0, label : this.folder });

            this.currentStep = 1;
            this._showSelectFile();
        }

        else if (this.file) {
            this._addBreadcrumb({ step: 1, label : this.file});

            this.currentStep = 2;
            this._showSelectSeries();
        }

        else {
            this.currentStep = 0;
            this._showSelectFolder();
        }
    },

    _back : function() {
        var previousStep = this.currentStep - 1;

        if (previousStep < 0) {
            return;
        }

        var previous = this._getContext(previousStep);

        this._removeBreadcrumbs(this.currentStep);
        this.currentStep = previousStep;
        this._showButtons(previous);

        previous.view.call(this);
    },

    _next : function(skip) {
        skip = skip || 0;
        skip = isNaN(skip) ? 0 : skip;
        skip++;

        var current = this.steps[this.currentStep];

        if (!current.callback.call(this, this.currentStep)) {
            return;
        }

        var nextStep = this.currentStep + skip;
        this.currentStep = nextStep;

        if (nextStep >= this.steps.length) {
            return;
        }

        var next = this._getContext(nextStep);

        this._showButtons(next);

        next.view.call(this);
    },

    _jump : function (view, options) {
        var model = options.model;
        this._removeBreadcrumbs(model.id + 1);
        this.currentStep = model.id;

        var step = this.steps[model.id];
        step.view.call(this);
    },

    _getContext : function (index) {
        var context = this.steps[index];
        return _.extend({showNextButton : false}, context);
    },

    _showButtons : function (context) {
        if (this.currentStep === 0) {
            this.ui.backButton.hide();
        }

        else {
            this.ui.backButton.show();
        }

        if (context.showNextButton) {
            this.ui.nextButton.show();
        }

        else {
            this.ui.nextButton.hide();
        }

        if (this.currentStep === this.steps.length - 1) {
            this.ui.importButton.show();
        }
    },

    _showBreadcrumbs : function () {
        this.breadcrumbCollection = new BreadcrumbCollection();

        this.breadcrumbView = new BreadcrumbView({ collection: this.breadcrumbCollection});
        this.listenTo(this.breadcrumbView, 'itemview:breadcrumb:clicked', this._jump);

        this.breadcrumb.show(this.breadcrumbView);
    },

    _addBreadcrumb : function (options) {
        options = options || {};

        var step = options.step === undefined ? this.currentStep : options.step;
        var current = options.current || false;

        this.breadcrumbCollection.add(new BreadcrumbModel({ id: step, label: options.label, active: current }), { merge : true });
    },

    _removeBreadcrumbs : function (step) {
        this.breadcrumbCollection.remove(_.filter(this.breadcrumbCollection.models, function (breadcrumb) {
            return breadcrumb.id >= step;
        }));
    },

    _showSelectFolder : function () {
        this._setSubtitle('Select Folder');

        this.currentView = new SelectFolderView();
        this.listenToOnce(this.currentView, 'fileSelected', function () { this._next(1); });
        this.listenToOnce(this.currentView, 'automaticImport', this._automaticImport);

        //TODO: Need to figure out how to confirm after selecting the series
//        this.listenToOnce(this.currentView, 'selectSeries', function () { this._next(1); });

        this.workspace.show(this.currentView);
    },

    _showSelectFile : function () {
        this._setSubtitle('Select File');

        this.currentView = new SelectFileLayout({ path : this.folder });
        this.listenToOnce(this.currentView, 'selectionChanged', this._next);
        this.workspace.show(this.currentView);
    },

    _showSelectSeries : function () {
        this._setSubtitle('Select Series');

        this.currentView = new SelectSeriesLayout();
        this.listenToOnce(this.currentView, 'selectionChanged', this._next);
        this.workspace.show(this.currentView);
    },

    _showSelectSeason : function () {
        this._setSubtitle('Select Season');

        var seasonCollection = new SeasonCollection(this.series.get('seasons'));

        this.currentView = new SelectSeasonView({
            selectable : true,
            collection : seasonCollection
        });

        this.listenToOnce(this.currentView, 'selectionChanged', this._next);

        this.workspace.show(this.currentView);
    },

    _showSelectEpisodes : function () {
        this._setSubtitle('Select Episodes');

        this.currentView = new SelectEpisodeLayout({ series: this.series, season: this.season });
        this.workspace.show(this.currentView);
    },

    _showSelectQuality : function () {
        this._setSubtitle('Select Quality');

        var profileSchemaCollection = new ProfileSchemaCollection();
        profileSchemaCollection.fetch();

        this.listenTo(profileSchemaCollection, 'sync', function (){
            var profile = profileSchemaCollection.first();
            var qualities = _.map(profile.get('items'), 'quality');

            this.currentView = new SelectQualityView({
                selectable : true,
                collection : new Backbone.Collection(qualities)
            });

            this.listenToOnce(this.currentView, 'selectionChanged', this._next);

            this.workspace.show(this.currentView);
        });
    },

    _showSummary : function () {
        this._setSubtitle('Confirm');

        this.currentView = new SummaryView({
            file     : this.file,
            series   : this.series.toJSON(),
            season   : this.season.toJSON(),
            episodes : this.episodes,
            quality  : this.quality.toJSON()
        });

        this.workspace.show(this.currentView);
    },

    _storeFolder : function () {
        this.folder = this.currentView.path();
        this._addBreadcrumb({ label : this.folder });

        return true;
    },

    _storeFile : function () {
        var file = this.currentView.path();
        this.file = file.path;
        this._addBreadcrumb({ label : file.relativePath });

        return true;
    },

    _storeSeries : function () {
        this.series = this.currentView.getSelectedSeries();
        this._addBreadcrumb({ label : this.series.get('title') });

        return true;
    },

    _storeSeason : function () {
        this.season = this.currentView.getSelectedModel();
        this._addBreadcrumb({ label : 'Season ' + this.season.get('seasonNumber') });

        return true;
    },

    _storeEpisodes : function () {
        this.episodes = this.currentView.getSelectedEpisodes();

        if (this.episodes.length === 0) {
            Messenger.show({
                message : 'At least one episode must be selected',
                type    : 'error'
            });

            return false;
        }

        var text = '';

        if (this.episodes.length === 1) {
            text = 'Episode ' + this.episodes[0].get('episodeNumber');
        }

        else {
            text = 'Episodes ' + _.map(this.episodes, function (episode) {
                return episode.get('episodeNumber');
            }).join(', ');
        }

        this._addBreadcrumb({ label : text });
        return true;
    },

    _storeQuality : function () {
        this.quality = this.currentView.getSelectedModel();
        this._addBreadcrumb({ label : this.quality.get('name') });

        return true;
    },

    _automaticImport : function () {
        CommandController.Execute('downloadedEpisodesScan', {
            name        : 'downloadedEpisodesScan',
            path        : this.currentView.path(),
            sendUpdates : true
        });

        vent.trigger(vent.Commands.CloseModalCommand);
    },

    _import : function () {
        //TODO: Import, cleanup folder (should delete folder),
        window.alert('Importing!');

        vent.trigger(vent.Commands.CloseModalCommand);
    },

    _setSubtitle : function (subtitle) {
        this.ui.subtitle.text('- ' + subtitle);
        this._addBreadcrumb({ label: subtitle, current : true });
    }
});