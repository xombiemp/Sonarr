var SelectSeriesCollectionView = require('backbone.collectionview');
var SelectSeriesItemView = require('./SelectSeriesItemView');

module.exports = SelectSeriesCollectionView.extend({
    className : 'select-series-list',
    modelView : SelectSeriesItemView
});