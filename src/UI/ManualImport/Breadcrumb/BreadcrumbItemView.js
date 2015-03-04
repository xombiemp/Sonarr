var Marionette = require('marionette');

module.exports = Marionette.ItemView.extend({
    tagName   : 'li',
    template  : 'ManualImport/breadcrumb/BreadcrumbItemViewTemplate',

    events : {
        'click' : '_click'
    },

    initialize : function () {
        this.listenTo(this.model, 'change', this.render);
    },

    onRender : function () {
        if (this.model.get('active')) {
            this.$el.addClass('active');
        }
    },

    _click : function (e) {
        e.preventDefault();
        e.stopImmediatePropagation();

        this.trigger('breadcrumb:clicked', { model : this.model });
    }
});