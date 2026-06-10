const path = require("path");

module.exports = {
    mode: 'development',
    entry: './wwwroot/js/site.js',
    output: {
        filename: 'site.bundle.js',
        path: path.resolve(__dirname, 'wwwroot/js'),
    },
    devtool: 'source-map',
    resolve: {
        extensions: ['.js'],
    },
};