var SelectQualityCollectionView = require('backbone.collectionview');
var SelectQualityItemView = require('./SelectQualityItemView');

module.exports = SelectQualityCollectionView.extend({
    className : 'select-Quality-list',
    modelView : SelectQualityItemView
});