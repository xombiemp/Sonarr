var SelectFileCollectionView = require('backbone.collectionview');
var SelectFileItemView = require('./SelectFileItemView');

module.exports = SelectFileCollectionView.extend({
    className : 'select-file-list',
    modelView : SelectFileItemView
});