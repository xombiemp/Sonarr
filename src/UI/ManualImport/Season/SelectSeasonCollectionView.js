var SelectSeasonCollectionView = require('backbone.collectionview');
var SelectSeasonItemView = require('./SelectSeasonItemView');

module.exports = SelectSeasonCollectionView.extend({
    className : 'select-season-list',
    modelView : SelectSeasonItemView
});