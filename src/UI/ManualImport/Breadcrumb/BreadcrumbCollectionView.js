var Marionette = require('marionette');
var BreadcrumbItemView = require('./BreadcrumbItemView');

module.exports = Marionette.CollectionView.extend({
    tagName   : 'ol',
    className : 'breadcrumb',
    itemView  : BreadcrumbItemView
});