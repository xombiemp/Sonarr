var Backbone = require('backbone');
var BreadcrumbModel = require('./BreadcrumbModel');

module.exports = Backbone.Collection.extend({
    model : BreadcrumbModel
});