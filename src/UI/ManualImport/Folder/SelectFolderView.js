var Marionette = require('marionette');
require('../../Mixins/FileBrowser');

module.exports = Marionette.ItemView.extend({
    template : 'ManualImport/Folder/SelectFolderViewTemplate',

    ui : {
        path : '.x-path'
    },

    events: {
        'filebrowser:fileselected .x-path' : '_fileSelected',
        'click .x-automatic-import' : '_automaticImport'
    },

    onRender : function() {
        this.ui.path.fileBrowser({ showFiles: true });
    },

    path : function() {
        return this.ui.path.val();
    },

    _fileSelected : function () {
        this.trigger('fileSelected');
    },

    _automaticImport : function () {
        if (this.ui.path.val()) {
            this.trigger('automaticImport');
        }
    }
});
