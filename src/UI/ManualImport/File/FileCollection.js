var Backbone = require('backbone');
var FileModel = require('./FileModel');

module.exports = Backbone.Collection.extend({
    url       : window.NzbDrone.ApiRoot + '/filesystem/mediafiles',
    model     : FileModel,

    originalFetch : Backbone.Collection.prototype.fetch,

    initialize : function(options) {
        this.path = options.path;
    },

    fetch : function(options) {
        if (!this.path) {
            throw 'path is required';
        }

        if (!options) {
            options = {};
        }

        options.data = { path : this.path };

        return this.originalFetch.call(this, options);
    }
});