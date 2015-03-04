var _ = require('underscore');
var Marionette = require('marionette');
var FileCollection = require('./FileCollection');
var SelectFileView = require('./SelectFileCollectionView');
var LoadingView = require('../../Shared/LoadingView');

module.exports = Marionette.Layout.extend({
    template  : 'ManualImport/File/SelectFileLayoutTemplate',

    regions : {
        files : '.x-files'
    },

    initialize : function(options) {
        this.fileCollection = new FileCollection({ path : options.path });
    },

    onRender : function() {
        this.files.show(new LoadingView());

        this.fileCollection.fetch();

        this.listenToOnce(this.fileCollection, 'sync', function () {

            this.fileView = new SelectFileView({
                selectable : true,
                collection : this.fileCollection
            });

            this.listenToOnce(this.fileView, 'selectionChanged', function () {
                this.trigger('selectionChanged');
            });

            this.files.show(this.fileView);
        });
    },

    path : function () {
        return this.fileView.getSelectedModel().toJSON();
    }
});