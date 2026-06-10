# WebOpenObserveDemo

# Install Opentelemetry dependencies and build tools

from root: 
```shell
cd WebOpenObserveDemo
npm install -y
npm start
```

# Add dependencies
package.json file
```json
{
  "name": "webopenobservedemo",
  "version": "1.0.0",
  "dependencies": {
    "@opentelemetry/api": "^1.8.0",
    "@opentelemetry/api-logs": "^0.50.0",
    "@opentelemetry/core": "^1.23.0",
    "@opentelemetry/exporter-logs-otlp-http": "^0.50.0",
    "@opentelemetry/exporter-metrics-otlp-http": "^0.50.0",
    "@opentelemetry/exporter-trace-otlp-http": "^0.50.0",
    "@opentelemetry/instrumentation": "^0.50.0",
    "@opentelemetry/instrumentation-document-load": "^0.36.0",
    "@opentelemetry/instrumentation-fetch": "^0.50.0",
    "@opentelemetry/instrumentation-user-interaction": "^0.36.0",
    "@opentelemetry/instrumentation-xml-http-request": "^0.50.0",
    "@opentelemetry/resources": "^1.23.0",
    "@opentelemetry/sdk-logs": "^0.50.0",
    "@opentelemetry/sdk-metrics": "^1.23.0",
    "@opentelemetry/sdk-trace-base": "^1.23.0",
    "@opentelemetry/sdk-trace-web": "^1.23.0",
    "@opentelemetry/semantic-conventions": "^1.23.0"
  },
  "devDependencies": {
    "clean-webpack-plugin": "4.0.0",
    "css-loader": "7.1.4",
    "html-webpack-plugin": "5.6.7",
    "mini-css-extract-plugin": "2.10.2",
    "parcel": "^2.16.4",
    "ts-loader": "9.5.7",
    "typescript": "6.0.3",
    "webpack": "^5.107.2",
    "webpack-cli": "5.1.4"
  },
  "description": "",
  "main": "wwwroot\\js\\site.js",
  "scripts": {
    "build": "webpack --mode=development --watch",
    "release": "webpack --mode=production",
    "publish": "npm run release && dotnet publish -c Release",
    "test": "echo \"Error: no test specified\" && exit 1"
  },
  "keywords": [],
  "author": "",
  "license": "ISC"
}
```

# Compile bundle file
npx webpack
